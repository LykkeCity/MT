using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Log;
using MarginTrading.Core;
using MarginTrading.Services.Events;

namespace MarginTrading.Services
{
    public class AggregatedOrderBook : IAggregatedOrderBook,
        IEventConsumer<OrderBookChangeEventArgs>
    {
        private readonly IEventChannel<BestPriceChangeEventArgs> _bestPriceChangEventChannel;
        private readonly ILog _log;
        private readonly IQuoteCacheService _quoteCache;
        private readonly Dictionary<string, SortedDictionary<double, OrderBookLevel>> _buy = new Dictionary<string, SortedDictionary<double, OrderBookLevel>>();
        private readonly Dictionary<string, SortedDictionary<double, OrderBookLevel>> _sell = new Dictionary<string, SortedDictionary<double, OrderBookLevel>>();

        private static readonly object Sync = new object();

        public AggregatedOrderBook(IEventChannel<BestPriceChangeEventArgs> bestPriceEventChannel, ILog log,
            IQuoteCacheService quoteCache)
        {
            _bestPriceChangEventChannel = bestPriceEventChannel;
            _log = log;
            _quoteCache = quoteCache;
        }

        public List<OrderBookLevel> GetBuy(string instrumentId)
        {
            lock (Sync)
                return !_buy.ContainsKey(instrumentId) ? new List<OrderBookLevel>() : _buy[instrumentId].Values.ToList();
        }

        public List<OrderBookLevel> GetSell(string instrumentId)
        {
            lock (Sync)
                return !_sell.ContainsKey(instrumentId) ? new List<OrderBookLevel>() : _sell[instrumentId].Values.ToList();
        }

        private void UpdateAggregatedOrderBook(OrderBookLevel orderBookItem)
        {
            var source = orderBookItem.Direction == OrderDirection.Buy ? _buy : _sell;

            if (!source.ContainsKey(orderBookItem.Instrument))
                source.Add(orderBookItem.Instrument,
                    orderBookItem.Direction == OrderDirection.Buy
                        ? new SortedDictionary<double, OrderBookLevel>(
                            new ReverseComparer<double>(Comparer<double>.Default))
                        : new SortedDictionary<double, OrderBookLevel>());

            var orderBookItems = source[orderBookItem.Instrument];

            if (orderBookItem.Volume == 0)
                orderBookItems.Remove(orderBookItem.Price);
            else if (orderBookItems.ContainsKey(orderBookItem.Price))
                orderBookItems[orderBookItem.Price] = orderBookItem;
            else
                orderBookItems.Add(orderBookItem.Price, orderBookItem);
        }

        private InstrumentBidAskPair CalculateBidAskPair(string instrumentId)
        {
            if (!_buy.ContainsKey(instrumentId) || !_sell.ContainsKey(instrumentId))
            {
                _log.WriteInfoAsync(nameof(AggregatedOrderBook), "CalculateBidAskPair",
                    $"Buy: {_buy.ToJson()}, Sell: {_sell.ToJson()}",
                    $"No full price for {instrumentId}.");
                return null;
            }
                
            var buyBook = _buy[instrumentId];
            if (0 == buyBook.Count)
            {
                _log.WriteInfoAsync(nameof(AggregatedOrderBook), "CalculateBidAskPair",
                    $"Buy: {_buy.ToJson()}, Sell: {_sell.ToJson()}",
                    $"BuyBook for {instrumentId} is empty.");
                return null;
            }
                
            var sellBook = _sell[instrumentId];
            if (0 == sellBook.Count)
            {
                _log.WriteInfoAsync(nameof(AggregatedOrderBook), "CalculateBidAskPair",
                    $"Buy: {_buy.ToJson()}, Sell: {_sell.ToJson()}",
                    $"SellBook for {instrumentId} is empty.");
                return null;
            }

            return new InstrumentBidAskPair
            {
                Bid = buyBook.Values.First().Price,
                Ask = sellBook.Values.First().Price,
                Instrument = instrumentId,
                Date = DateTime.UtcNow
            };
        }

        int IEventConsumer.ConsumerRank => 10;
        public void ConsumeEvent(object sender, OrderBookChangeEventArgs ea)
        {
            lock (Sync)
            {
                foreach (var values in ea.Buy.Values)
                    foreach (var orderBookItem in values.Values)
                        UpdateAggregatedOrderBook(orderBookItem);

                foreach (var values in ea.Sell.Values)
                    foreach (var orderBookItem in values.Values)
                        UpdateAggregatedOrderBook(orderBookItem);

                foreach (string instrumentId in ea.GetChangedInstruments())
                {
                    var newPrice = CalculateBidAskPair(instrumentId);
                    if (newPrice != null)
                    {
                        InstrumentBidAskPair currentPrice;
                        if (_quoteCache.TryGetQuoteById(instrumentId, out currentPrice))
                        {
                            if (Math.Abs(newPrice.Ask - currentPrice.Ask) > 0
                                || Math.Abs(newPrice.Bid - currentPrice.Bid) > 0)
                            {
                                _bestPriceChangEventChannel.SendEvent(this, new BestPriceChangeEventArgs(newPrice));
                            }
                        }
                        else
                        {
                            _bestPriceChangEventChannel.SendEvent(this, new BestPriceChangeEventArgs(newPrice));
                        }
                    }
                }
            }
        }
    }
}