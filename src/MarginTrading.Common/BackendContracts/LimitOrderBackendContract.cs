using System;

namespace MarginTrading.Common.BackendContracts
{
    public class LimitOrderBackendContract
    {
        public string Id { get; set; }
        public string MarketMakerId { get; set; }
        public string Instrument { get; set; }
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public DateTime CreateDate { get; set; }
        public MatchedOrderBackendContract[] MatchedOrders { get; set; }
    }

    public class MatchedOrderBackendContract
    {
        public string OrderId { get; set; }
        public string MarketMakerId { get; set; }
        public decimal LimitOrderLeftToMatch { get; set; }
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public DateTime MatchedDate { get; set; }
    }
}
