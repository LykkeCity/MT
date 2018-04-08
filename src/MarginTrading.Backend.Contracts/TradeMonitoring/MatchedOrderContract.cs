using System;
using System.Collections.Generic;
using System.Text;

namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
    public class MatchedOrderContract
    {
        public string OrderId { get; set; }
        public string MarketMakerId { get; set; }
        public decimal LimitOrderLeftToMatch { get; set; }
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public string ClientId { get; set; }
        public DateTime MatchedDate { get; set; }
    }
}
