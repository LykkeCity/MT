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
        private readonly Dictionary<string, SortedDictionary<double, OrderBookLevel>> _buy = new Dictionary<string, SortedDictionary<double, OrderBookLevel>>();
        private readonly Dictionary<string, SortedDictionary<double, OrderBookLevel>> _sell = new Dictionary<string, SortedDictionary<double, OrderBookLevel>>();
        private readonly Dictionary<string, InstrumentBidAskPair> _bidAskPairs = new Dictionary<string, InstrumentBidAskPair>();

        private static readonly object Sync = new object();

        public AggregatedOrderBook(IEventChannel<BestPriceChangeEventArgs> bestPriceEventChannel, ILog log)
        {
            _bestPriceChangEventChannel = bestPriceEventChannel;
            _log = log;
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

        public double? GetPriceFor(string instrumentId, OrderDirection orderType)
        {
            lock (Sync)
            {
                if (!_bidAskPairs.ContainsKey(instrumentId))
                    return null;
                return _bidAskPairs[instrumentId].GetPriceForOrderType(orderType);
            }
        }
        
        private bool CheckIfPriceChanged(InstrumentBidAskPair newPrice)
        {
            if (null == newPrice)
                return false;

            if (!_bidAskPairs.ContainsKey(newPrice.Instrument))
            {
                _bidAskPairs.Add(newPrice.Instrument, newPrice);
                return true;
            }

            var oldPrice = _bidAskPairs[newPrice.Instrument];
            if (oldPrice.Ask != newPrice.Ask || oldPrice.Bid != newPrice.Bid)
            {
                _bidAskPairs[newPrice.Instrument] = newPrice;
                return true;
            }

            return false;
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
                return null;
            }
                
            var buyBook = _buy[instrumentId];
            if (0 == buyBook.Count)
                return null;

            var sellBook = _sell[instrumentId];
            if (0 == sellBook.Count)
                return null;

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
                    if (CheckIfPriceChanged(newPrice))
                    {
                        _bestPriceChangEventChannel.SendEvent(this, new BestPriceChangeEventArgs(newPrice));
                    }
                    else
                    {
                        _log.WriteWarningAsync(nameof(AggregatedOrderBook), "OrderBookChangeEvent", ea.ToJson(),
                            $"Price for {instrumentId} not changed. Buy: {GetBuy(instrumentId).ToJson()}, Sell: {GetSell(instrumentId).ToJson()}");
                    }
                }
            }
        }
    }
}