using System;

namespace MarginTrading.Common.BackendContracts
{
    public class LimitOrderBackendContract
    {
        public string Id { get; set; }
        public string MarketMakerId { get; set; }
        public string Instrument { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }
        public DateTime CreateDate { get; set; }
        public MatchedOrderBackendContract[] MatchedOrders { get; set; }
    }

    public class MatchedOrderBackendContract
    {
        public string OrderId { get; set; }
        public string MarketMakerId { get; set; }
        public double LimitOrderLeftToMatch { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }
        public DateTime MatchedDate { get; set; }
    }
}
