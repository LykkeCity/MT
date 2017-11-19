using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.MatchedOrders;

namespace MarginTrading.Backend.Core.Orderbooks
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

        public IEnumerable<MatchedOrder> Match(Order order, OrderDirection orderTypeToMatch, decimal volumeToMatch, int maxMarketMakerLimitOrderAge)
        {
            if (volumeToMatch == 0)
                yield break;

            var source = orderTypeToMatch == OrderDirection.Buy ? Buy : Sell;
            volumeToMatch = Math.Abs(volumeToMatch);
            var minMarketMakerOrderDate = maxMarketMakerLimitOrderAge > 0
                ? DateTime.UtcNow.AddSeconds(-maxMarketMakerLimitOrderAge)
                : DateTime.MinValue;

            foreach (KeyValuePair<decimal, List<LimitOrder>> pair in source)
                foreach (var limitOrder in pair.Value.OrderBy(item => item.CreateDate))
                {
                    if (!string.IsNullOrEmpty(limitOrder.MarketMakerId) &&
                        limitOrder.CreateDate < minMarketMakerOrderDate)
                        continue;
                    
                    var matchedVolume = Math.Min(limitOrder.GetRemainingVolume(), volumeToMatch);
                    yield return new MatchedOrder
                    {
                        OrderId = limitOrder.Id,
                        MarketMakerId = limitOrder.MarketMakerId,
                        LimitOrderLeftToMatch = Math.Round(Math.Abs(matchedVolume - limitOrder.GetRemainingVolume()),
                            MarginTradingHelpers.VolumeAccuracy),
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
                {
                    source[matchedOrder.Price].Remove(bookOrder);
                    
                    if (!source[matchedOrder.Price].Any())
                        source.Remove(matchedOrder.Price);
                }
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
}
