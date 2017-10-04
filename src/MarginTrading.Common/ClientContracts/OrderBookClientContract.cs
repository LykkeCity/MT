using System;
using System.Collections.Generic;

namespace MarginTrading.Common.ClientContracts
{
    public class OrderBookClientContract
    {
        public Dictionary<decimal, LimitOrderClientContract[]> Buy { get; set; }
        public Dictionary<decimal, LimitOrderClientContract[]> Sell { get; set; }
    }

    public class LimitOrderClientContract
    {
        public string Id { get; set; }
        public string MarketMakerId { get; set; }
        public string Instrument { get; set; }
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public DateTime CreateDate { get; set; }
        public MatchedOrderClientContract[] MatchedOrders { get; set; }
    }

    public class MatchedOrderClientContract
    {
        public string OrderId { get; set; }
        public string MarketMakerId { get; set; }
        public decimal LimitOrderLeftToMatch { get; set; }
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public DateTime MatchedDate { get; set; }
    }
}
