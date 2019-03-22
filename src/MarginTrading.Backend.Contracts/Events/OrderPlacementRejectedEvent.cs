using System;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.TradeMonitoring;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Events
{
    /// <summary>
    /// Event of order placement request being rejected without placing an order.
    /// </summary>
    [MessagePackObject]
    public class OrderPlacementRejectedEvent
    {
        /// <summary>
        /// Id of the process which caused parameter change.
        /// </summary>
        [Key(0)]
        public string CorrelationId { get; set; }
        
        /// <summary>
        /// Time of event generation.
        /// </summary>
        [Key(1)]
        public DateTime EventTimestamp { get; set; }
        
        /// <summary>
        /// Order placement request which was rejected.
        /// </summary>
        [Key(2)]
        public OrderPlaceRequest OrderPlaceRequest { get; set; }
        
        /// <summary>
        /// Order reject reason
        /// </summary>
        [Key(3)]
        public OrderRejectReasonContract RejectReason { get; set; }
        
        /// <summary>
        /// Order reject reason text
        /// </summary>
        [Key(4)]
        public string RejectReasonText { get; set; }
    }
}