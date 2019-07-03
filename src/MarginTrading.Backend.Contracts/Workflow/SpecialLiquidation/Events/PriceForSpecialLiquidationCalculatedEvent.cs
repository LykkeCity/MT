using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events
{
    /// <summary>
    /// Quote request for particular instrument and volume succeeded.
    /// </summary>
    [MessagePackObject]
    public class PriceForSpecialLiquidationCalculatedEvent
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
        /// Instrument  - should not be used!
        /// </summary>
        [Key(3)]
        [Obsolete]
        public string Instrument { get; set; }
        
        /// <summary>
        /// Position volume - should not be used!
        /// </summary>
        [Key(4)]
        [Obsolete]
        public decimal Volume { get; set; }
        
        /// <summary>
        /// Requested price value
        /// </summary>
        [Key(5)]
        public decimal Price { get; set; }
        
        /// <summary>
        /// Streaming number of request. Increases in case when price arrived, but volume has changed. 
        /// </summary>
        [Key(6)]
        public int RequestNumber { get; set; }
    }
}