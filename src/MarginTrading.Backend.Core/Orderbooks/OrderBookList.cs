using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.Helpers;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Core.Orderbooks
{
    /// <summary>
    /// List of order books
    /// </summary>
    /// <remarks>
    /// Not thread-safe!
    /// </remarks>
    public class OrderBookList
    {
        private readonly MarginSettings _marginSettings;

        public OrderBookList(MarginSettings marginSettings)
        {
            _marginSettings = marginSettings;
        }
        
        private Dictionary<string, OrderBook> _orderBooks;

        public Dictionary<string, OrderBook> GetOrderBookState()
        {
            return _orderBooks.ToDictionary(p => p.Key, p => p.Value.Clone());
        }

        public void Init(Dictionary<string, OrderBook> orderBook)
        {
            _orderBooks = orderBook ?? new Dictionary<string, OrderBook>();
        }

        public MatchedOrderCollection Match(Order order, OrderDirection orderTypeToMatch, decimal volumeToMatch)
        {
            if (!_orderBooks.ContainsKey(order.Instrument))
                return new MatchedOrderCollection();

            return new MatchedOrderCollection(_orderBooks[order.Instrument]
                .Match(order, orderTypeToMatch, volumeToMatch, _marginSettings.MaxMarketMakerLimitOrderAge));
        }

        public void Update(Order order, OrderDirection orderTypeToMatch, IEnumerable<MatchedOrder> matchedOrders)
        {
            if (_orderBooks.TryGetValue(order.Instrument, out var orderBook))
            {
                orderBook.Update(order, orderTypeToMatch, matchedOrders);
            }
        }

        public OrderBook GetOrderBook(string instrumentId)
        {
            var orderbook = _orderBooks.GetValueOrDefault(instrumentId, k => new OrderBook(instrumentId));

            return orderbook.Clone();
        }

        public IEnumerable<LimitOrder> DeleteMarketMakerOrders(string marketMakerId, string[] idsToDelete)
        {
            return _orderBooks.SelectMany(o => o.Value.DeleteMarketMakerOrders(marketMakerId, idsToDelete));
        }

        public void DeleteAllOrdersByMarketMaker(string marketMakerId, bool deleteAllBuy, bool deleteAllSell)
        {
            foreach (var orderbook in _orderBooks)
            {
                orderbook.Value.DeleteAllOrdersByMarketMaker(marketMakerId, deleteAllBuy, deleteAllSell);
            }
        }

        public void DeleteAllBuyOrdersByMarketMaker(string marketMakerId, IReadOnlyList<string> instruments)
        {
            foreach (var instrument in instruments)
            {
                if (_orderBooks.TryGetValue(instrument, out var orderBook))
                {
                    orderBook.DeleteAllOrdersByMarketMaker(marketMakerId, true, false);
                }
            }
        }

        public void DeleteAllSellOrdersByMarketMaker(string marketMakerId, IReadOnlyList<string> instruments)
        {
            foreach (var instrument in instruments)
            {
                if (_orderBooks.TryGetValue(instrument, out var orderBook))
                {
                    orderBook.DeleteAllOrdersByMarketMaker(marketMakerId, false, true);
                }
            }
        }

        public void AddMarketMakerOrders(IReadOnlyList<LimitOrder> ordersToAdd)
        {
            foreach (var order in ordersToAdd)
            {
                if (!_orderBooks.TryGetValue(order.Instrument, out var orderBook))
                {
                    orderBook = new OrderBook(order.Instrument);
                    _orderBooks.Add(order.Instrument, orderBook);
                }

                orderBook.AddMarketMakerOrder(order);
            }
        }
        
        public InstrumentBidAskPair GetBestPrice(string instrument)
        {
            if (_orderBooks.TryGetValue(instrument, out var orderbook))
            {
                return orderbook.BestPrice;
            }

            return null;
        }
    }
}