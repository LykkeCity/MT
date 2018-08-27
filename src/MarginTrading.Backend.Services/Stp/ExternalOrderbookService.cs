using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Helpers;
using MarginTrading.Common.Services;
using MarginTrading.SettingsService.Contracts.AssetPair;

namespace MarginTrading.Backend.Services.Stp
{
    public class ExternalOrderbookService : IExternalOrderbookService
    {
        private readonly IEventChannel<BestPriceChangeEventArgs> _bestPriceChangeEventChannel;
        private readonly IDateService _dateService;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly ICqrsSender _cqrsSender;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ILog _log;

        public ExternalOrderbookService(
            IEventChannel<BestPriceChangeEventArgs> bestPriceChangeEventChannel,
            IDateService dateService,
            IAssetPairsCache assetPairsCache,
            ICqrsSender cqrsSender,
            IIdentityGenerator identityGenerator,
            ILog log)
        {
            _bestPriceChangeEventChannel = bestPriceChangeEventChannel;
            _dateService = dateService;
            _assetPairsCache = assetPairsCache;
            _cqrsSender = cqrsSender;
            _identityGenerator = identityGenerator;
            _log = log;
        }

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

        public List<(string source, decimal? price)> GetPricesForExecution(string assetPairId, decimal volume,
            bool validateOppositeDirectionVolume)
        {
            return _orderbooks.TryReadValue(assetPairId, (dataExist, assetPair, orderbooks)
                => dataExist
                    ? orderbooks.Select(p => (p.Key,
                        MatchBestPriceForOrderExecution(p.Value, volume, validateOppositeDirectionVolume))).ToList()
                    : null);
        }

        public decimal? GetPriceForPositionClose(string assetPairId, decimal volume, string externalProviderId)
        {
            decimal? CalculatePriceForClose(Dictionary<string, ExternalOrderBook> orderbooks)
            {
                if (!orderbooks.TryGetValue(externalProviderId, out var orderBook))
                {
                    return null;
                }

                return MatchBestPriceForPositionClose(orderBook, volume);
            }

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
            bool validateOpositeDirectionVolume)
        {
            var direction = volume.GetOrderDirection();

            var price = externalOrderBook.GetMatchedPrice(Math.Abs(volume), direction);

            if (price != null && validateOpositeDirectionVolume)
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

        public void SetOrderbook(ExternalOrderBook orderbook)
        {
            if (!ValidateOrderbook(orderbook) 
                || !CheckZeroQuote(orderbook))
                return;

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

            _bestPriceChangeEventChannel.SendEvent(this, new BestPriceChangeEventArgs(bba));
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
                orderbook.Bids.RemoveAll(e => e == null || e.Price <= 0 || e.Volume == 0);
                //ValidatePricesSorted(orderbook.Bids, false);
                
                orderbook.Asks.RequiredNotNullOrEmpty("orderbook.Asks");
                orderbook.Asks.RemoveAll(e => e == null || e.Price <= 0 || e.Volume == 0);
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
        
        private bool CheckZeroQuote(ExternalOrderBook orderbook)
        {
            var isOrderbookValid = orderbook.Asks.Count > 0 && orderbook.Bids.Count > 0;//after validations
            
            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(orderbook.AssetPairId);
            if (assetPair == null)
            {
                return isOrderbookValid;
            }
            
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
                }
            }

            return isOrderbookValid;
        }
    }
}