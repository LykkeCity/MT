using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Core
{
    public class OrderBook
    {
        public string Instrument { get; set; }

        public SortedDictionary<double, List<LimitOrder>> Buy { get; set; } =
            new SortedDictionary<double, List<LimitOrder>>(new ReverseComparer<double>(Comparer<double>.Default));

        public SortedDictionary<double, List<LimitOrder>> Sell { get; set; } =
            new SortedDictionary<double, List<LimitOrder>>();

        public OrderBook Clone()
        {
            var res = new OrderBook
            {
                Instrument = Instrument,
                Buy =
                    new SortedDictionary<double, List<LimitOrder>>(new ReverseComparer<double>(Comparer<double>.Default)),
                Sell = new SortedDictionary<double, List<LimitOrder>>()
            };

            FillOrders(res.Buy, Buy);
            FillOrders(res.Sell, Sell);

            return res;
        }

        private void FillOrders(SortedDictionary<double, List<LimitOrder>> dst,
            SortedDictionary<double, List<LimitOrder>> src)
        {
            foreach (var pair in src)
            {
                var orders = new List<LimitOrder>();

                foreach (LimitOrder order in pair.Value)
                {
                    orders.Add(new LimitOrder
                    {
                        Id = order.Id,
                        Instrument = order.Instrument,
                        Volume = order.Volume,
                        Price = order.Price,
                        MatchedOrders = order.MatchedOrders
                            .Select(
                                m =>
                                    new MatchedOrder
                                    {
                                        MatchedDate = m.MatchedDate,
                                        OrderId = m.OrderId,
                                        MarketMakerId = m.MarketMakerId,
                                        Price = m.Price,
                                        Volume = m.Volume
                                    })
                            .ToList(),
                        CreateDate = order.CreateDate,
                        MarketMakerId = order.MarketMakerId

                    });
                }

                dst.Add(pair.Key, orders);
            }
        }

        public double GetRemainingVolume(OrderDirection orderType, double price)
        {
            var source = orderType == OrderDirection.Buy ? Buy : Sell;

            if (!source.ContainsKey(price))
                return 0;

            return source[price].Sum(x => x.GetRemainingVolume());
        }

        public IEnumerable<MatchedOrder> Match(Order order, OrderDirection orderTypeToMatch, double volumeToMatch)
        {
            if (volumeToMatch == 0)
                yield break;

            var source = orderTypeToMatch == OrderDirection.Buy ? Buy : Sell;
            volumeToMatch = Math.Abs(volumeToMatch);

            foreach (KeyValuePair<double, List<LimitOrder>> pair in source)
                foreach (var limitOrder in pair.Value.OrderBy(item => item.CreateDate))
                {
                    var matchedVolume = Math.Min(limitOrder.GetRemainingVolume(), volumeToMatch);
                    yield return new MatchedOrder
                    {
                        OrderId = limitOrder.Id,
                        MarketMakerId = limitOrder.MarketMakerId,
                        LimitOrderLeftToMatch = Math.Abs(matchedVolume - limitOrder.GetRemainingVolume()),
                        Volume = matchedVolume,
                        MatchedDate = DateTime.UtcNow,
                        Price = pair.Key,
                        ClientId = limitOrder.MarketMakerId
                    };

                    volumeToMatch = Math.Round(volumeToMatch - matchedVolume, MarginTradingHelpers.VolumeAccuracy);
                    if (volumeToMatch <= 0)
                        yield break;
                }
        }

        public void Update(Order order, OrderDirection orderTypeToMatch, IEnumerable<MatchedOrder> matchedOrders)
        {
            var source = orderTypeToMatch == OrderDirection.Buy ? Buy : Sell;
            foreach (MatchedOrder matchedOrder in matchedOrders)
            {
                var bookOrder = source[matchedOrder.Price].First(item => item.Id == matchedOrder.OrderId);

                bookOrder.MatchedOrders.Add(new MatchedOrder
                {
                    OrderId = order.Id,
                    MarketMakerId = matchedOrder.MarketMakerId,
                    Volume = matchedOrder.Volume,
                    MatchedDate = matchedOrder.MatchedDate,
                    Price = matchedOrder.Price,
                    ClientId = matchedOrder.ClientId
                });

                if (bookOrder.GetIsFullfilled())
                    source[matchedOrder.Price].Remove(bookOrder);
            }
        }

        public IEnumerable<LimitOrder> DeleteMarketMakerOrders(string marketMakerId, string[] idsToDelete)
        {
            var result = new List<LimitOrder>();
            var buyOrders = Buy.DeleteMarketMakerOrders(marketMakerId, idsToDelete);
            var sellOrders = Sell.DeleteMarketMakerOrders(marketMakerId, idsToDelete);

            result.AddRange(buyOrders);
            result.AddRange(sellOrders);

            return result;
        }

        public IEnumerable<LimitOrder> DeleteAllOrdersByMarketMaker(string marketMakerId, bool deleteAllBuy, bool deleteAllSell)
        {
            var result = new List<LimitOrder>();
            var buyOrders = new List<LimitOrder>(); 
            var sellOrders = new List<LimitOrder>(); 

            if (deleteAllBuy)
                buyOrders = Buy.DeleteAllOrdersByMarketMaker(marketMakerId);

            if (deleteAllSell)
                sellOrders = Sell.DeleteAllOrdersByMarketMaker(marketMakerId);

            result.AddRange(buyOrders);
            result.AddRange(sellOrders);

            return result;
        }
    }

    public sealed class ReverseComparer<T> : IComparer<T>
    {
        private readonly IComparer<T> original;

        public ReverseComparer(IComparer<T> original)
        {
            this.original = original;
        }

        public int Compare(T left, T right)
        {
            return original.Compare(right, left);
        }
    }

    public class AggregatedOrderInfo
    {
        public double Price { get; set; }
        public double Volume { get; set; }
        public bool IsBuy { get; set; }
    }

    public class AggregatedOrderBookItem
    {
        public double Price { get; set; }
        public double Volume { get; set; }
    }

    public static class OrderBookExt
    {
        public static AggregatedOrderInfo Aggregate(this OrderBook src, OrderDirection direction)
        {
            var price = direction == OrderDirection.Buy
                ? src.Buy.Keys.FirstOrDefault()
                : src.Sell.Keys.FirstOrDefault();

            double volume = direction == OrderDirection.Buy
                ? src.Buy.Values.FirstOrDefault()?.Sum(item => item.Volume) ?? 0
                : src.Sell.Values.FirstOrDefault()?.Sum(item => item.Volume) ?? 0;

            return new AggregatedOrderInfo
            {
                Price = price,
                Volume = volume,
                IsBuy = direction == OrderDirection.Buy
            };
        }

        public static List<LimitOrder> DeleteMarketMakerOrders(this SortedDictionary<double, List<LimitOrder>> src,
            string marketMakerId, string[] idsToDelete)
        {
            var result = new List<LimitOrder>();

            foreach (List<LimitOrder> limitOrders in src.Values)
            {
                result.AddRange(limitOrders.Where(x => x.MarketMakerId == marketMakerId && (idsToDelete == null || idsToDelete.Contains(x.Id))));
                limitOrders.RemoveAll(x => x.MarketMakerId == marketMakerId && (idsToDelete == null || idsToDelete.Contains(x.Id)));
            }

            src.RemoveEmptyKeys();

            return result;
        }

        public static List<LimitOrder> DeleteAllOrdersByMarketMaker(this SortedDictionary<double, List<LimitOrder>> src,
            string marketMakerId)
        {
            var result = new List<LimitOrder>();

            foreach (List<LimitOrder> limitOrders in src.Values)
            {
                result.AddRange(limitOrders.Where(x => x.MarketMakerId == marketMakerId));
                limitOrders.RemoveAll(x => x.MarketMakerId == marketMakerId);
            }

            src.RemoveEmptyKeys();

            return result;
        }

        public static LimitOrder AddMarketMakerOrder(this SortedDictionary<double, List<LimitOrder>> src,
            LimitOrder order)
        {
            if (!src.ContainsKey(order.Price))
                src.Add(order.Price, new List<LimitOrder>());

            var existingOrder = src[order.Price].FirstOrDefault(
                            item => item.MarketMakerId == order.MarketMakerId);

            if (existingOrder != null)
            {
                existingOrder.Volume = order.Volume;
                return existingOrder;
            }

            src[order.Price].Add(order);
            return order;
        }

        public static void RemoveEmptyKeys(this SortedDictionary<double, List<LimitOrder>> src)
        {
            var emptyKeys = src.Where(pair => pair.Value.Count == 0)
                    .Select(pair => pair.Key)
                    .ToList();

            foreach (var key in emptyKeys)
            {
                src.Remove(key);
            }
        }
    }

    //TODO: check concurent work
    public class OrderBookList : IEnumerable<KeyValuePair<string, OrderBook>>
    {
        private readonly IInstrumentsCache _instrumentsCache;

        private Dictionary<string, OrderBook> _orderBooks;

        public OrderBookList(IInstrumentsCache instrumentsCache)
        {
            _instrumentsCache = instrumentsCache;
        }

        public Dictionary<string, OrderBook> GetOrderBookState()
        {
            return _orderBooks.ToDictionary(p => p.Key, p => p.Value.Clone());
        }

        public double GetRemainingVolume(string instrumentId, OrderDirection orderType,
            double price)
        {
            var instrument = _instrumentsCache.GetInstrumentById(instrumentId);

            if (!_orderBooks.ContainsKey(instrumentId))
                return 0;

            return _orderBooks[instrumentId].GetRemainingVolume(orderType, price);
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

        public IEnumerable<MatchedOrder> Match(Order order, OrderDirection orderTypeToMatch, double volumeToMatch)
        {
            if (!_orderBooks.ContainsKey(order.Instrument))
                return Array.Empty<MatchedOrder>();

            return _orderBooks[order.Instrument].Match(order, orderTypeToMatch, volumeToMatch);
        }

        public void Update(Order order, OrderDirection orderTypeToMatch, IEnumerable<MatchedOrder> matchedOrders)
        {
            if (!_orderBooks.ContainsKey(order.Instrument))
                return;

            _orderBooks[order.Instrument].Update(order, orderTypeToMatch, matchedOrders);
        }

        public OrderListPair GetAllLimitOrders(string instrumentId)
        {
            return new OrderListPair()
            {
                Buy =
                    _orderBooks.ContainsKey(instrumentId)
                        ? _orderBooks[instrumentId].Buy.Values.SelectMany(x => x).ToArray()
                        : Array.Empty<LimitOrder>(),
                Sell =
                    _orderBooks.ContainsKey(instrumentId)
                        ? _orderBooks[instrumentId].Buy.Values.SelectMany(x => x).ToArray()
                        : Array.Empty<LimitOrder>()
            };
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

        public IEnumerable<LimitOrder> DeleteAllBuyOrdersByMarketMaker(string marketMakerId, string[] instruments)
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

        public IEnumerable<LimitOrder> DeleteAllSellOrdersByMarketMaker(string marketMakerId, string[] instruments)
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

        public List<LimitOrder> AddMarketMakerOrders(LimitOrder[] ordersToAdd)
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

    public class OrderListPair
    {
        public LimitOrder[] Buy { get; set; }
        public LimitOrder[] Sell { get; set; }
    }

    public class OrderBookLevel
    {
        public string Instrument { get; set; }
        public double Price { get; set; }
        public double Volume { get; set; }
        public OrderDirection Direction { get; set; }

        public static OrderBookLevel Create(LimitOrder order)
        {
            return new OrderBookLevel
            {
                Direction = order.GetOrderType(),
                Instrument = order.Instrument,
                Volume = order.Volume,
                Price = order.Price
            };
        }

        public static OrderBookLevel Create(MatchedOrder order, OrderDirection direction, string instrument)
        {
            return new OrderBookLevel
            {
                Direction = direction,
                Instrument = instrument,
                Volume = order.Volume,
                Price = order.Price
            };
        }

        public static OrderBookLevel CreateDeleted(LimitOrder order)
        {
            return new OrderBookLevel
            {
                Direction = order.GetOrderType(),
                Instrument = order.Instrument,
                Volume = 0,
                Price = order.Price
            };
        }
    }
}
