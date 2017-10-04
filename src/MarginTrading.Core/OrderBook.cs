using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Core.Helpers;
using MarginTrading.Core.MatchedOrders;

namespace MarginTrading.Core
{
    public class OrderBook
    {
        public string Instrument { get; set; }

        public SortedDictionary<decimal, List<LimitOrder>> Buy { get; set; } =
            new SortedDictionary<decimal, List<LimitOrder>>(new ReverseComparer<decimal>(Comparer<decimal>.Default));

        public SortedDictionary<decimal, List<LimitOrder>> Sell { get; set; } =
            new SortedDictionary<decimal, List<LimitOrder>>();

        public OrderBook Clone()
        {
            var res = new OrderBook
            {
                Instrument = Instrument,
                Buy =
                    new SortedDictionary<decimal, List<LimitOrder>>(new ReverseComparer<decimal>(Comparer<decimal>.Default)),
                Sell = new SortedDictionary<decimal, List<LimitOrder>>()
            };

            FillOrders(res.Buy, Buy);
            FillOrders(res.Sell, Sell);

            return res;
        }

        private void FillOrders(SortedDictionary<decimal, List<LimitOrder>> dst,
            SortedDictionary<decimal, List<LimitOrder>> src)
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
                        MatchedOrders = new MatchedOrderCollection(order.MatchedOrders
                            .Select(
                                m =>
                                    new MatchedOrder
                                    {
                                        MatchedDate = m.MatchedDate,
                                        OrderId = m.OrderId,
                                        MarketMakerId = m.MarketMakerId,
                                        Price = m.Price,
                                        Volume = m.Volume
                                    })),
                        CreateDate = order.CreateDate,
                        MarketMakerId = order.MarketMakerId

                    });
                }

                dst.Add(pair.Key, orders);
            }
        }

        public IEnumerable<MatchedOrder> Match(Order order, OrderDirection orderTypeToMatch, decimal volumeToMatch)
        {
            if (volumeToMatch == 0)
                yield break;

            var source = orderTypeToMatch == OrderDirection.Buy ? Buy : Sell;
            volumeToMatch = Math.Abs(volumeToMatch);

            foreach (KeyValuePair<decimal, List<LimitOrder>> pair in source)
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
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public bool IsBuy { get; set; }
    }

    public class AggregatedOrderBookItem
    {
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
    }

    public static class OrderBookExt
    {
        public static AggregatedOrderInfo Aggregate(this OrderBook src, OrderDirection direction)
        {
            var price = direction == OrderDirection.Buy
                ? src.Buy.Keys.FirstOrDefault()
                : src.Sell.Keys.FirstOrDefault();

            decimal volume = direction == OrderDirection.Buy
                ? src.Buy.Values.FirstOrDefault()?.Sum(item => item.Volume) ?? 0
                : src.Sell.Values.FirstOrDefault()?.Sum(item => item.Volume) ?? 0;

            return new AggregatedOrderInfo
            {
                Price = price,
                Volume = volume,
                IsBuy = direction == OrderDirection.Buy
            };
        }

        public static List<LimitOrder> DeleteMarketMakerOrders(this SortedDictionary<decimal, List<LimitOrder>> src,
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

        public static List<LimitOrder> DeleteAllOrdersByMarketMaker(this SortedDictionary<decimal, List<LimitOrder>> src,
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

        public static LimitOrder AddMarketMakerOrder(this SortedDictionary<decimal, List<LimitOrder>> src,
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

        public static void RemoveEmptyKeys(this SortedDictionary<decimal, List<LimitOrder>> src)
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
        private readonly IAssetPairsCache _assetPairsCache;

        private Dictionary<string, OrderBook> _orderBooks;

        public OrderBookList(IAssetPairsCache assetPairsCache)
        {
            _assetPairsCache = assetPairsCache;
        }

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
                .Match(order, orderTypeToMatch, volumeToMatch).ToList());
        }

        public void Update(Order order, OrderDirection orderTypeToMatch, IEnumerable<MatchedOrder> matchedOrders)
        {
            if (!_orderBooks.ContainsKey(order.Instrument))
                return;

            _orderBooks[order.Instrument].Update(order, orderTypeToMatch, matchedOrders);
        }

        public OrderListPair GetAllLimitOrders(string instrumentId)
        {
            var orderBook = _orderBooks.GetValueOrDefault(instrumentId, k => new OrderBook());
            return new OrderListPair
            {
                Buy = orderBook.Buy.Values.SelectMany(x => x).ToArray(),
                Sell = orderBook.Sell.Values.SelectMany(x => x).ToArray(),
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

    public class OrderListPair
    {
        public LimitOrder[] Buy { get; set; }
        public LimitOrder[] Sell { get; set; }
    }

    public class OrderBookLevel
    {
        public string Instrument { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
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
