// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.MarginTrading.OrderBookService.Contracts;
using Lykke.MarginTrading.OrderBookService.Contracts.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Helpers;
using MarginTrading.Common.Services;
using MarginTrading.AssetService.Contracts.AssetPair;
using MoreLinq;

namespace MarginTrading.Backend.Services.Stp
{
    public class LightweightExternalOrderbookService : IExternalOrderbookService
    {
        private readonly IEventChannel<BestPriceChangeEventArgs> _bestPriceChangeEventChannel;
        private readonly IOrderBookProviderApi _orderBookProviderApi;
        private readonly IDateService _dateService;
        private readonly IConvertService _convertService;
        private readonly IScheduleSettingsCacheService _scheduleSettingsCache;
        private readonly IAssetPairDayOffService _assetPairDayOffService;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly ICqrsSender _cqrsSender;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ILog _log;
        private readonly string _defaultExternalExchangeId;
        private readonly OrderbookValidationSettings _orderbookValidation;

        /// <summary>
        /// External orderbooks cache (AssetPairId, Orderbook)
        /// </summary>
        /// <remarks>
        /// We assume that AssetPairId is unique in LegalEntity + STP mode. <br/>
        /// Please use <see cref="ReadWriteLockedDictionary{TKey,TValue}.TryReadValue{TResult}"/> for this purpose.
        /// </remarks>
        private readonly ReadWriteLockedDictionary<string, ExternalOrderBook> _orderbooks =
            new ReadWriteLockedDictionary<string, ExternalOrderBook>();

        public LightweightExternalOrderbookService(
            IEventChannel<BestPriceChangeEventArgs> bestPriceChangeEventChannel,
            IOrderBookProviderApi orderBookProviderApi,
            IDateService dateService,
            IConvertService convertService,
            IScheduleSettingsCacheService scheduleSettingsCache,
            IAssetPairDayOffService assetPairDayOffService,
            IAssetPairsCache assetPairsCache,
            ICqrsSender cqrsSender,
            IIdentityGenerator identityGenerator,
            ILog log,
            MarginTradingSettings marginTradingSettings)
        {
            _bestPriceChangeEventChannel = bestPriceChangeEventChannel;
            _orderBookProviderApi = orderBookProviderApi;
            _dateService = dateService;
            _convertService = convertService;
            _scheduleSettingsCache = scheduleSettingsCache;
            _assetPairDayOffService = assetPairDayOffService;
            _assetPairsCache = assetPairsCache;
            _cqrsSender = cqrsSender;
            _identityGenerator = identityGenerator;
            _log = log;
            _defaultExternalExchangeId = string.IsNullOrEmpty(marginTradingSettings.DefaultExternalExchangeId)
                ? "Default"
                : marginTradingSettings.DefaultExternalExchangeId;
            _orderbookValidation = marginTradingSettings.OrderbookValidation;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var orderBooks = (await _orderBookProviderApi.GetOrderBooks())
                    .GroupBy(x => x.AssetPairId)
                    .Select(x => _convertService.Convert<ExternalOrderBookContract, ExternalOrderBook>(
                        x.MaxBy(o => o.ReceiveTimestamp)))
                    .ToList();

                foreach (var externalOrderBook in orderBooks)
                {
                    SetOrderbook(externalOrderBook);
                }
                
                await _log.WriteInfoAsync(nameof(LightweightExternalOrderbookService), nameof(InitializeAsync),
                    $"External order books cache initialized with {orderBooks.Count} items from OrderBooks Service");
            }
            catch (Exception exception)
            {
                await _log.WriteWarningAsync(nameof(LightweightExternalOrderbookService), nameof(InitializeAsync),
                    "Failed to initialize cache from OrderBook Service", exception);
            }
        }

        public List<(string source, decimal? price)> GetOrderedPricesForExecution(string assetPairId, decimal volume,
            bool validateOppositeDirectionVolume)
        {
            if (!_orderbooks.TryGetValue(assetPairId, out var orderBook))
            {
                return null;
            }

            var price = MatchBestPriceForOrderExecution(orderBook, volume, validateOppositeDirectionVolume);

            if (price == null)
                return null;
            
            return new List<(string source, decimal? price)>
            {
                (orderBook.ExchangeName, price)
            };
        }

        public decimal? GetPriceForPositionClose(string assetPairId, decimal volume, string externalProviderId)
        {
            if (!_orderbooks.TryGetValue(assetPairId, out var orderBook))
            {
                return null;
            }

            return MatchBestPriceForPositionClose(orderBook, volume);
        }

        //TODO: understand which orderbook should be used (best price? aggregated?)
        public ExternalOrderBook GetOrderBook(string assetPairId)
        {
            return _orderbooks.TryGetValue(assetPairId, out var orderBook) ? orderBook : null;
        }

        private static decimal? MatchBestPriceForOrderExecution(ExternalOrderBook externalOrderBook, decimal volume,
            bool validateOppositeDirectionVolume)
        {
            var direction = volume.GetOrderDirection();

            var price = externalOrderBook.GetMatchedPrice(volume, direction);

            if (price != null && validateOppositeDirectionVolume)
            {
                var closePrice = externalOrderBook.GetMatchedPrice(volume, direction.GetOpositeDirection());

                //if no liquidity for close, should not use price for open
                if (closePrice == null)
                    return null;
            }

            return price;
        }

        private static decimal? MatchBestPriceForPositionClose(ExternalOrderBook externalOrderBook, decimal volume)
        {
            var direction = volume.GetClosePositionOrderDirection();
            
            return externalOrderBook.GetMatchedPrice(Math.Abs(volume), direction);
        }

        public List<ExternalOrderBook> GetOrderBooks()
        {
            return _orderbooks
                .Select(x => x.Value)
                .ToList();
        }

        public void SetOrderbook(ExternalOrderBook orderbook)
        {
            var isEodOrderbook = orderbook.ExchangeName == ExternalOrderbookService.EodExternalExchange;

            if (_orderbookValidation.ValidateInstrumentStatusForEodQuotes && isEodOrderbook ||
                _orderbookValidation.ValidateInstrumentStatusForTradingQuotes && !isEodOrderbook)
            {
                var isDayOff = _assetPairDayOffService.IsDayOff(orderbook.AssetPairId);
            
                // we should process normal orderbook only if instrument is currently tradable
                if (_orderbookValidation.ValidateInstrumentStatusForTradingQuotes && isDayOff && !isEodOrderbook)    
                {
                    return;
                }

                // and process EOD orderbook only if instrument is currently not tradable
                if (_orderbookValidation.ValidateInstrumentStatusForEodQuotes && !isDayOff && isEodOrderbook)
                {
                    //log current schedule for the instrument
                    var schedule = _scheduleSettingsCache.GetMarketTradingScheduleByAssetPair(orderbook.AssetPairId);

                    _log.WriteWarning("EOD quotes processing", $"Current schedule for the instrument's market: {schedule.ToJson()}",
                        $"EOD quote for {orderbook.AssetPairId} is skipped, because instrument is within trading hours");

                    return;
                }
            }
            
            if (!CheckZeroQuote(orderbook, isEodOrderbook))
                return;
            
            orderbook.ApplyExchangeIdFromSettings(_defaultExternalExchangeId);

            var bba = orderbook.GetBestPrice();

            _orderbooks.AddOrUpdate(orderbook.AssetPairId,a => orderbook, (s, book) => orderbook);
            
            _bestPriceChangeEventChannel.SendEvent(this, new BestPriceChangeEventArgs(bba, isEodOrderbook));
        }

        private bool CheckZeroQuote(ExternalOrderBook orderbook, bool isEodOrderbook)
        {
            // TODO: the code below supposes we have only one quote in orderbook
            var hasZeroVolume = orderbook.Asks[0].Volume == 0 || orderbook.Bids[0].Volume == 0;
            var hasZeroPrice = orderbook.Asks[0].Price == 0 || orderbook.Bids[0].Price == 0;
            var isOrderbookValid = !hasZeroVolume && !hasZeroPrice;
            
            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(orderbook.AssetPairId);
            if (assetPair == null)
            {
                return isOrderbookValid;
            }
            
            //EOD quotes should not change asset pair state
            if (isEodOrderbook)
                return isOrderbookValid;
            
            if (!isOrderbookValid)
            {
                // suspend instrument in case of zero volumes only
                if (!assetPair.IsSuspended && hasZeroVolume)
                {
                    assetPair.IsSuspended = true;//todo apply changes to trading engine
                    _cqrsSender.SendCommandToSettingsService(new SuspendAssetPairCommand
                    {
                        AssetPairId = assetPair.Id,
                        OperationId = _identityGenerator.GenerateGuid(),
                    });
                    
                    _log.Info($"Suspending instrument {assetPair.Id}", context: orderbook.ToContextData()?.ToJson());
                }
            }
            else
            {
                if (assetPair.IsSuspended)
                {
                    assetPair.IsSuspended = false;//todo apply changes to trading engine
                    _cqrsSender.SendCommandToSettingsService(new UnsuspendAssetPairCommand
                    {
                        AssetPairId = assetPair.Id,
                        OperationId = _identityGenerator.GenerateGuid(),
                    }); 
                    
                    _log.Info($"Un-suspending instrument {assetPair.Id}", context: orderbook.ToContextData()?.ToJson());
                }
            }

            return isOrderbookValid;
        }
    }
}