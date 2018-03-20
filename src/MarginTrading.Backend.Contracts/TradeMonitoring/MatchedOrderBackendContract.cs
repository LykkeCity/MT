using System;

namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
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
