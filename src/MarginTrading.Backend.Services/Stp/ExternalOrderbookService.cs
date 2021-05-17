// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
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
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Helpers;
using MarginTrading.Common.Services;
using MarginTrading.AssetService.Contracts.AssetPair;
using MoreLinq;

namespace MarginTrading.Backend.Services.Stp
{
    public class ExternalOrderbookService : IExternalOrderbookService
    {
        private readonly IEventChannel<BestPriceChangeEventArgs> _bestPriceChangeEventChannel;
        private readonly IOrderBookProviderApi _orderBookProviderApi;
        private readonly IDateService _dateService;
        private readonly IConvertService _convertService;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly ICqrsSender _cqrsSender;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ILog _log;
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly IAssetPairDayOffService _assetPairDayOffService;
        private readonly IScheduleSettingsCacheService _scheduleSettingsCache;

        public const string EodExternalExchange = "EOD";
        
        /// <summary>
        /// External orderbooks cache (AssetPairId, (Source, Orderbook))
        /// </summary>
        /// <remarks>
        /// We assume that AssetPairId is unique in LegalEntity + STP mode. <br/>
        /// Note that it is unsafe to even read the inner dictionary without locking.
        /// Please use <see cref="ReadWriteLockedDictionary{TKey,TValue}.TryReadValue{TResult}"/> for this purpose.
        /// </remarks>
        private readonly ReadWriteLockedDictionary<string, Dictionary<string, ExternalOrderBook>> _orderbooks =
            new ReadWriteLockedDictionary<string, Dictionary<string, ExternalOrderBook>>();

        public ExternalOrderbookService(
            IEventChannel<BestPriceChangeEventArgs> bestPriceChangeEventChannel,
            IOrderBookProviderApi orderBookProviderApi,
            IDateService dateService,
            IConvertService convertService,
            IAssetPairDayOffService assetPairDayOffService,
            IScheduleSettingsCacheService scheduleSettingsCache,
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
            _assetPairDayOffService = assetPairDayOffService;
            _scheduleSettingsCache = scheduleSettingsCache;
            _assetPairsCache = assetPairsCache;
            _cqrsSender = cqrsSender;
            _identityGenerator = identityGenerator;
            _log = log;
            _marginTradingSettings = marginTradingSettings;
        }

        public void Start()
        {
            try
            {
                var orderBooks = _orderBookProviderApi.GetOrderBooks().GetAwaiter().GetResult()
                    .GroupBy(x => x.AssetPairId)
                    .Select(x => _convertService.Convert<ExternalOrderBookContract, ExternalOrderBook>(
                        x.MaxBy(o => o.ReceiveTimestamp)))
                    .ToList();

                foreach (var externalOrderBook in orderBooks)
                {
                    SetOrderbook(externalOrderBook);
                }
                
                _log.WriteInfo(nameof(ExternalOrderbookService), nameof(Start),
                    $"External order books cache initialized with {orderBooks.Count} items from OrderBooks Service");
            }
            catch (Exception exception)
            {
                _log.WriteWarning(nameof(ExternalOrderbookService), nameof(Start),
                    "Failed to initialize cache from OrderBook Service", exception);
            }
        }

        public List<(string source, decimal? price)> GetOrderedPricesForExecution(string assetPairId, decimal volume,
            bool validateOppositeDirectionVolume)
        {
            return _orderbooks.TryReadValue(assetPairId, (dataExist, assetPair, orderbooks)
                =>
            {
                if (!dataExist) 
                    return null;
                
                var result = orderbooks.Select(p => (p.Key,
                        MatchBestPriceForOrderExecution(p.Value, volume, validateOppositeDirectionVolume)))
                    .Where(p => p.Item2 != null).ToArray();

                return volume.GetOrderDirection() == OrderDirection.Buy
                    ? result.OrderBy(tuple => tuple.Item2).ToList()
                    : result.OrderByDescending(tuple => tuple.Item2).ToList();
             });
        }

        public decimal? GetPriceForPositionClose(string assetPairId, decimal volume, string externalProviderId)
        {
            decimal? CalculatePriceForClose(Dictionary<string, ExternalOrderBook> orderbooks)
                => !orderbooks.TryGetValue(externalProviderId, out var orderBook)
                    ? null
                    : MatchBestPriceForPositionClose(orderBook, volume);
            
            return _orderbooks.TryReadValue(assetPairId, (dataExist, assetPair, orderbooks)
                => dataExist ? CalculatePriceForClose(orderbooks) : null);
        }

        //TODO: understand which orderbook should be used (best price? aggregated?)
        public ExternalOrderBook GetOrderBook(string assetPairId)
        {
            return _orderbooks.TryReadValue(assetPairId,
                (exists, assetPair, orderbooks) => orderbooks.Values.FirstOrDefault());
        }

        private static decimal? MatchBestPriceForOrderExecution(ExternalOrderBook externalOrderBook, decimal volume,
            bool validateOppositeDirectionVolume)
        {
            var direction = volume.GetOrderDirection();

            var price = externalOrderBook.GetMatchedPrice(Math.Abs(volume), direction);

            if (price != null && validateOppositeDirectionVolume)
            {
                var closePrice = externalOrderBook.GetMatchedPrice(Math.Abs(volume), direction.GetOpositeDirection());

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
                .Select(x => x.Value.Values.MaxBy(o => o.Timestamp))
                .ToList();
        }

        public void SetOrderbook(ExternalOrderBook orderbook)
        {
            var isEodOrderbook = orderbook.ExchangeName == EodExternalExchange;

            if (_marginTradingSettings.OrderbookValidation.ValidateInstrumentStatusForEodQuotes && isEodOrderbook ||
                _marginTradingSettings.OrderbookValidation.ValidateInstrumentStatusForTradingQuotes && !isEodOrderbook)
            {
                var isDayOff = _assetPairDayOffService.IsDayOff(orderbook.AssetPairId);

                // we should process normal orderbook only if asset is currently tradeable
                if (_marginTradingSettings.OrderbookValidation.ValidateInstrumentStatusForTradingQuotes &&
                    isDayOff &&
                    !isEodOrderbook)
                {
                    return;
                }

                // and process EOD orderbook only if instrument is currently not tradable
                if (_marginTradingSettings.OrderbookValidation.ValidateInstrumentStatusForEodQuotes &&
                    !isDayOff &&
                    isEodOrderbook)
                {
                    //log current schedule for the instrument
                    var schedule = _scheduleSettingsCache.GetMarketTradingScheduleByAssetPair(orderbook.AssetPairId);

                    _log.WriteWarning("EOD quotes processing", $"Current schedule: {schedule.ToJson()}",
                        $"EOD quote for {orderbook.AssetPairId} is skipped, because instrument is within trading hours");

                    return;
                }
            }

            if (!ValidateOrderbook(orderbook) 
                || !CheckZeroQuote(orderbook, isEodOrderbook))
                return;
            
            orderbook.ApplyExchangeIdFromSettings(_marginTradingSettings.DefaultExternalExchangeId);

            var bba = new InstrumentBidAskPair
            {
                Bid = 0,
                Ask = decimal.MaxValue,
                Date = _dateService.Now(),
                Instrument = orderbook.AssetPairId
            };

            Dictionary<string, ExternalOrderBook> UpdateOrderbooksDictionary(string assetPairId,
                Dictionary<string, ExternalOrderBook> dict)
            {
                dict[orderbook.ExchangeName] = orderbook;
                foreach (var pair in dict.Values.RequiredNotNullOrEmptyCollection(nameof(dict)))
                {
                    // guaranteed to be sorted best first
                    var bestBid = pair.Bids.First().Price;
                    var bestAsk = pair.Asks.First().Price;
                    if (bestBid > bba.Bid)
                        bba.Bid = bestBid;

                    if (bestAsk < bba.Ask)
                        bba.Ask = bestAsk;
                }

                return dict;
            }

            _orderbooks.AddOrUpdate(orderbook.AssetPairId,
                k => UpdateOrderbooksDictionary(k, new Dictionary<string, ExternalOrderBook>()),
                UpdateOrderbooksDictionary);

            _bestPriceChangeEventChannel.SendEvent(this, new BestPriceChangeEventArgs(bba, isEodOrderbook));
        }
        
        //TODO: sort prices of uncomment validation
        private bool ValidateOrderbook(ExternalOrderBook orderbook)
        {
            try
            {
                orderbook.AssetPairId.RequiredNotNullOrWhiteSpace("orderbook.AssetPairId");
                orderbook.ExchangeName.RequiredNotNullOrWhiteSpace("orderbook.ExchangeName");
                orderbook.RequiredNotNull(nameof(orderbook));
                
                orderbook.Bids.RequiredNotNullOrEmpty("orderbook.Bids");
                orderbook.Bids = orderbook.Bids.Where(e => e != null && e.Price > 0 && e.Volume != 0).ToArray();
                //ValidatePricesSorted(orderbook.Bids, false);
                
                orderbook.Asks.RequiredNotNullOrEmpty("orderbook.Asks");
                orderbook.Asks = orderbook.Asks.Where(e => e != null && e.Price > 0 && e.Volume != 0).ToArray();
                //ValidatePricesSorted(orderbook.Asks, true);

                return true;
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(ExternalOrderbookService), orderbook.ToJson(), e);
                return false;
            }
        }

        private void ValidatePricesSorted(IEnumerable<VolumePrice> volumePrices, bool ascending)
        {
            decimal? previous = null;
            foreach (var current in volumePrices.Select(p => p.Price))
            {
                if (previous != null && ascending ? current < previous : current > previous)
                {
                    throw new Exception("Prices should be sorted best first");
                }
                
                previous = current;
            }
        }
        
        private bool CheckZeroQuote(ExternalOrderBook orderbook, bool isEodOrderbook)
        {
            var isOrderbookValid = orderbook.Asks.Length > 0 && orderbook.Bids.Length > 0;//after validations
            
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
                if (!assetPair.IsSuspended)
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