using System;

namespace MarginTrading.Core.MatchedOrders
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
}