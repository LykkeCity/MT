using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events
{
    /// <summary>
    /// Trade request for particular instrument, volume and price successfully executed.
    /// </summary>
    [MessagePackObject]
    public class SpecialLiquidationOrderExecutedEvent
    {
        /// <summary>
        /// Operation Id
        /// </summary>
        [Key(0)]
        public string OperationId { get; set; }
        
        /// <summary>
        /// Event creation time
        /// </summary>
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        /// <summary>
        /// Market Maker Id on which the order was executed
        /// </summary>
        [Key(2)]
        public string MarketMakerId { get; set; }
    }
}