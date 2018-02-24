using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Helpers;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Stp
{
    public class ExternalOrderBooksList
    {
        private readonly IEventChannel<BestPriceChangeEventArgs> _bestPriceChangeEventChannel;
        private readonly IDateService _dateService;
        private readonly ILog _log;

        public ExternalOrderBooksList(IEventChannel<BestPriceChangeEventArgs> bestPriceChangeEventChannel,
            IDateService dateService, ILog log)
        {
            _bestPriceChangeEventChannel = bestPriceChangeEventChannel;
            _dateService = dateService;
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

        public List<(string source, decimal price)> GetPricesForMatch(IOrder order, bool isOpening)
        {
            return _orderbooks.TryReadValue(order.Instrument, (dataExist, assetPairId, orderbooks)
                => orderbooks.Select(p => (p.Key, MatchBestPriceForOrder(p.Value, order, isOpening)))).ToList();
        }

        private static decimal MatchBestPriceForOrder(ExternalOrderBook externalOrderbook, IOrder order, bool isOpening)
        {
            // todo: revise logic
            var direction = isOpening ? order.GetOrderType() : order.GetCloseType();
            return direction == OrderDirection.Buy
                ? externalOrderbook.Asks.First().Price
                : externalOrderbook.Bids.First().Price;
        }

        public void SetOrderbook(ExternalOrderBook orderbook)
        {
            if (!ValidateOrderbook(orderbook))
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

        private bool ValidateOrderbook(ExternalOrderBook orderbook)
        {
            try
            {
                orderbook.AssetPairId.RequiredNotNullOrWhiteSpace("orderbook.AssetPairId");
                orderbook.ExchangeName.RequiredNotNullOrWhiteSpace("orderbook.ExchangeName");
                orderbook.RequiredNotNull(nameof(orderbook));
                
                orderbook.Bids.RequiredNotNullOrEmpty("orderbook.Bids");
                orderbook.Bids.RemoveAll(e => e == null || e.Price <= 0 || e.Volume == 0);
                ValidatePricesSorted(orderbook.Bids, false);
                
                orderbook.Asks.RequiredNotNullOrEmpty("orderbook.Asks");
                orderbook.Asks.RemoveAll(e => e == null || e.Price <= 0 || e.Volume == 0);
                ValidatePricesSorted(orderbook.Asks, true);

                return true;
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(ExternalOrderBooksList), orderbook.ToJson(), e);
                return false;
            }
        }

        private void ValidatePricesSorted(IEnumerable<VolumePrice> volumePrices, bool ascending)
        {
            decimal? previous = null;
            foreach (var current in volumePrices.Select(p => p.Price))
            {
                if (previous != null && ascending ? current < previous : current > previous)
                    throw new Exception("Prices should be sorted best first");
                
                previous = current;
            }
        }
    }
}