using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.Orderbooks
{
    public class OrderBook
    {
        public string Instrument { get; }
        
        public SortedDictionary<decimal, List<LimitOrder>> Buy { get; private set; } 
            
        public SortedDictionary<decimal, List<LimitOrder>> Sell { get; private set; } 
        
        public InstrumentBidAskPair BestPrice { get; private set; }

        public OrderBook(string instrument)
        {
            Instrument = instrument;
            Buy = new SortedDictionary<decimal, List<LimitOrder>>(new ReverseComparer<decimal>(Comparer<decimal>.Default));
            Sell = new SortedDictionary<decimal, List<LimitOrder>>();
            BestPrice = new InstrumentBidAskPair {Instrument = instrument, Date = DateTime.UtcNow};
        }

        public OrderBook Clone()
        {
            var res = new OrderBook(Instrument)
            {
                Buy = new SortedDictionary<decimal, List<LimitOrder>>(
                        new ReverseComparer<decimal>(Comparer<decimal>.Default)),
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

                foreach (var order in pair.Value)
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

        public IEnumerable<MatchedOrder> Match(OrderDirection orderTypeToMatch, decimal volumeToMatch, int maxMarketMakerLimitOrderAge)
        {
            if (volumeToMatch == 0)
                yield break;

            var source = orderTypeToMatch == OrderDirection.Buy ? Buy : Sell;
            volumeToMatch = Math.Abs(volumeToMatch);
            var minMarketMakerOrderDate = maxMarketMakerLimitOrderAge > 0
                ? DateTime.UtcNow.AddSeconds(-maxMarketMakerLimitOrderAge)
                : DateTime.MinValue;

            foreach (var pair in source)
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
                        Price = pair.Key
                    };

                    volumeToMatch = Math.Round(volumeToMatch - matchedVolume, MarginTradingHelpers.VolumeAccuracy);
                    if (volumeToMatch <= 0)
                        yield break;
                }
        }

        public void Update(OrderDirection orderTypeToMatch, IEnumerable<MatchedOrder> matchedOrders)
        {
            var source = orderTypeToMatch == OrderDirection.Buy ? Buy : Sell;
            foreach (var matchedOrder in matchedOrders)
            {
                var bookOrder = source[matchedOrder.Price].First(item => item.Id == matchedOrder.OrderId);

                bookOrder.MatchedOrders.Add(matchedOrder);

                if (bookOrder.GetIsFullfilled())
                {
                    source[matchedOrder.Price].Remove(bookOrder);
                    
                    if (!source[matchedOrder.Price].Any())
                        source.Remove(matchedOrder.Price);
                }

                UpdateBestPrice();
            }
        }
        
        public void AddMarketMakerOrder(LimitOrder order)
        {
            var src = order.GetOrderDirection() == OrderDirection.Buy ? Buy : Sell;
            
            if (!src.ContainsKey(order.Price))
                src.Add(order.Price, new List<LimitOrder>());

            var existingOrder = src[order.Price].FirstOrDefault(
                item => item.MarketMakerId == order.MarketMakerId);

            if (existingOrder != null)
            {
                existingOrder.Volume = order.Volume;
            }

            src[order.Price].Add(order);

            UpdateBestPrice();
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

        private void UpdateBestPrice()
        {
            var newBestPrice = new InstrumentBidAskPair
            {
                Instrument = Instrument,
                Date = DateTime.UtcNow
            };

            if (Sell.Any())
            {
                var fl = Sell.First();
                newBestPrice.Ask = fl.Key;
                newBestPrice.AskFirstLevelVolume = fl.Value.Sum(o => Math.Abs(o.Volume));
            }
            else
            {
                newBestPrice.Ask = BestPrice?.Ask ?? 0;
            }
            
            if (Buy.Any())
            {
                var fl = Buy.First();
                newBestPrice.Bid = fl.Key;
                newBestPrice.BidFirstLevelVolume = fl.Value.Sum(o => Math.Abs(o.Volume));
            }
            else
            {
                newBestPrice.Bid = BestPrice?.Bid ?? 0;
            }
            
            if (newBestPrice.Ask > 0 && newBestPrice.Bid > 0)
                BestPrice = newBestPrice;
        }
    }
}
