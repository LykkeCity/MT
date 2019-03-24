using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Orders
{
    [MessagePackObject]
    public class OrderPlaceRequest
    {
        [Key(0)]
        public string AccountId { get; set; }
        [Key(1)]
        public string InstrumentId { get; set; }
        [Key(2)]
        public string ParentOrderId { get; set; } // null if basic, ParentOrderId if related
        [Key(3)]
        public string PositionId { get; set; }

        [Key(4)]
        public OrderDirectionContract Direction { get; set; }
        [Key(5)]
        public OrderTypeContract Type { get; set; }
        [Key(6)]
        public OriginatorTypeContract Originator { get; set; }

        [Key(7)]
        public decimal Volume { get; set; }
        [Key(8)]
        public decimal? Price { get; set; } // null for market
        
        [Key(9)]
        public decimal? StopLoss { get; set; }
        [Key(10)]
        public decimal? TakeProfit { get; set; }
        [Key(11)]
        public bool UseTrailingStop { get; set; }
        
        [Key(12)]
        public bool ForceOpen { get; set; }

        [Key(13)]
        public DateTime? Validity { get; set; } // null for market
        
        [Key(14)]
        public string AdditionalInfo { get; set; }

        /// <summary>
        /// The correlation identifier. Optional: if not passed will be auto-generated.  
        /// </summary>
        [Key(15)]
        public string CorrelationId { get; set; }
    }
}