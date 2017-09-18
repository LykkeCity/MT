using System;
using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Core
{
    public class MatchedOrder
    {
        public string OrderId { get; set; }
        public string MarketMakerId { get; set; }
        public double LimitOrderLeftToMatch { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }
        public string ClientId { get; set; }
        public DateTime MatchedDate { get; set; }

        public static MatchedOrder Create(MatchedOrder src)
        {
            return new MatchedOrder
            {
                OrderId = src.OrderId,
                MarketMakerId = src.MarketMakerId,
                LimitOrderLeftToMatch = src.LimitOrderLeftToMatch,
                Volume = src.Volume,
                Price = src.Price,
                ClientId = src.ClientId,
                MatchedDate = src.MatchedDate
            };
        }
    }

    public static class MatchedOrderExtension
    {
        public static double GetWeightedAveragePrice(this List<MatchedOrder> orders)
        {
            return orders.Sum(x => x.Price * x.Volume) / orders.Sum(x => x.Volume);
        }

        public static double GetTotalVolume(this IEnumerable<MatchedOrder> orders)
        {
            return orders.Sum(x => x.Volume);
        }

        public static LimitOrder CreateLimit(this MatchedOrder order, string instrument, OrderDirection direction)
        {
            return new LimitOrder
            {
                MarketMakerId = order.MarketMakerId,
                Instrument = instrument,
                Price = order.Price,
                Volume = direction == OrderDirection.Buy ? order.LimitOrderLeftToMatch : -order.LimitOrderLeftToMatch
            };
        }
    }
}