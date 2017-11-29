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
        
        private DateTime _lastUpdated = DateTime.UtcNow; 

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

                bookOrder.MatchedOrders.Add(matchedOrder);

                if (bookOrder.GetIsFullfilled())
                {
                    source[matchedOrder.Price].Remove(bookOrder);
                    
                    if (!source[matchedOrder.Price].Any())
                        source.Remove(matchedOrder.Price);
                }
                
                _lastUpdated = DateTime.UtcNow;
            }
        }

        public IEnumerable<LimitOrder> DeleteMarketMakerOrders(string marketMakerId, string[] idsToDelete)
        {
            var deletedBuy = Buy.DeleteMarketMakerOrders(marketMakerId, idsToDelete);
            var deletedSell = Sell.DeleteMarketMakerOrders(marketMakerId, idsToDelete);

            return deletedBuy.Concat(deletedSell);
        }

        public void DeleteAllOrdersByMarketMaker(string marketMakerId, bool deleteAllBuy, bool deleteAllSell)
        {
            if (deleteAllBuy)
                Buy.DeleteAllOrdersByMarketMaker(marketMakerId);

            if (deleteAllSell)
                Sell.DeleteAllOrdersByMarketMaker(marketMakerId);
        }

        public InstrumentBidAskPair GetBestPrice()
        {
            if (!Sell.Any() || !Buy.Any())
                return null;
            
            return new InstrumentBidAskPair
            {
                Instrument = Instrument,
                Ask = Sell.First().Key,
                Bid = Buy.First().Key,
                Date = _lastUpdated
            };
        }
    }
}
