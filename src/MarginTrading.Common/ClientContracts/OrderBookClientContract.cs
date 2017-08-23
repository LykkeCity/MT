using System;
using System.Collections.Generic;

namespace MarginTrading.Common.ClientContracts
{
    public class OrderBookClientContract
    {
        public Dictionary<double, LimitOrderClientContract[]> Buy { get; set; }
        public Dictionary<double, LimitOrderClientContract[]> Sell { get; set; }
    }

    public class LimitOrderClientContract
    {
        public string Id { get; set; }
        public string MarketMakerId { get; set; }
        public string Instrument { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }
        public DateTime CreateDate { get; set; }
        public MatchedOrderClientContract[] MatchedOrders { get; set; }
    }

    public class MatchedOrderClientContract
    {
        public string OrderId { get; set; }
        public string MarketMakerId { get; set; }
        public double LimitOrderLeftToMatch { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }
        public DateTime MatchedDate { get; set; }
    }
}
