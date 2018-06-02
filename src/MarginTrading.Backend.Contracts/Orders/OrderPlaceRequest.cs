using System;

namespace MarginTrading.Backend.Contracts.Orders
{
    public class OrderPlaceRequest
    {
        public string AccountId { get; set; }
        public string InstrumentId { get; set; }
        public string ParentOrderId { get; set; } // null if basic, ParentOrderId if related
        public string PositionId { get; set; }

        public OrderDirectionContract Direction { get; set; }
        public OrderTypeContract Type { get; set; }
        public OriginatorTypeContract Originator { get; set; }

        public decimal Volume { get; set; }
        public decimal? Price { get; set; } // null for market
        
        public decimal? StopLoss { get; set; }
        public decimal? TakeProfit { get; set; }
        
        public bool ForceOpen { get; set; }

        public DateTime? Validity { get; set; } // null for market

    }
}