using System.Collections;
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
    public class OrderBookList : IEnumerable<KeyValuePair<string, OrderBook>>
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, OrderBook>> GetEnumerator()
        {
            return _orderBooks.GetEnumerator();
        }

        public MatchedOrderCollection Match(Order order, OrderDirection orderTypeToMatch, decimal volumeToMatch)
        {
            if (!_orderBooks.ContainsKey(order.Instrument))
                return new MatchedOrderCollection();

            return new MatchedOrderCollection(_orderBooks[order.Instrument]
                .Match(order, orderTypeToMatch, volumeToMatch, _marginSettings.MaxMarketMakerLimitOrderAge).ToList());
        }

        public void Update(Order order, OrderDirection orderTypeToMatch, IEnumerable<MatchedOrder> matchedOrders)
        {
            if (!_orderBooks.ContainsKey(order.Instrument))
                return;

            _orderBooks[order.Instrument].Update(order, orderTypeToMatch, matchedOrders);
        }

        public OrderBook GetOrderBook(string instrumentId)
        {
            var orderbook = _orderBooks.GetValueOrDefault(instrumentId, k => new OrderBook());

            return orderbook.Clone();
        }

        public IEnumerable<LimitOrder> DeleteMarketMakerOrders(string marketMakerId, string[] idsToDelete)
        {
            foreach (var orderbook in _orderBooks)
            {
                var orders = orderbook.Value.DeleteMarketMakerOrders(marketMakerId, idsToDelete);
                foreach (var order in orders)
                    yield return order;
            }
        }

        public IEnumerable<LimitOrder> DeleteAllOrdersByMarketMaker(string marketMakerId, bool deleteAllBuy, bool deleteAllSell)
        {
            foreach (var orderbook in _orderBooks)
            {
                var orders = orderbook.Value.DeleteAllOrdersByMarketMaker(marketMakerId, deleteAllBuy, deleteAllSell);
                foreach (var order in orders)
                    yield return order;
            }
        }

        public IEnumerable<LimitOrder> DeleteAllBuyOrdersByMarketMaker(string marketMakerId, IReadOnlyList<string> instruments)
        {
            foreach (var instrument in instruments)
            {
                if (_orderBooks.ContainsKey(instrument))
                {
                    var orders = _orderBooks[instrument].DeleteAllOrdersByMarketMaker(marketMakerId, true, false);
                    foreach (var order in orders)
                        yield return order;
                }
            }
        }

        public IEnumerable<LimitOrder> DeleteAllSellOrdersByMarketMaker(string marketMakerId, IReadOnlyList<string> instruments)
        {
            foreach (var instrument in instruments)
            {
                if (_orderBooks.ContainsKey(instrument))
                {
                    var orders = _orderBooks[instrument].DeleteAllOrdersByMarketMaker(marketMakerId, false, true);
                    foreach (var order in orders)
                        yield return order;
                }
            }
        }

        public List<LimitOrder> AddMarketMakerOrders(IReadOnlyList<LimitOrder> ordersToAdd)
        {
            var result = new List<LimitOrder>();

            foreach (var order in ordersToAdd)
            {
                if (!_orderBooks.ContainsKey(order.Instrument))
                {
                    _orderBooks.Add(order.Instrument, new OrderBook { Instrument = order.Instrument });
                }

                var source = order.GetOrderType() == OrderDirection.Buy
                    ? _orderBooks[order.Instrument].Buy
                    : _orderBooks[order.Instrument].Sell;


                var addedOrder = source.AddMarketMakerOrder(order);
                result.Add(addedOrder);
            }

            return result;
        }
    }
}