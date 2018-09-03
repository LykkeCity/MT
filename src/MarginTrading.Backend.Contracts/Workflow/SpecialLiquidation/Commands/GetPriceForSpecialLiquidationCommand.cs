using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands
{
    /// <summary>
    /// Quote request for particular instrument and volume.
    /// </summary>
    [MessagePackObject]
    public class GetPriceForSpecialLiquidationCommand
    {
        /// <summary>
        /// Operation Id
        /// </summary>
        [Key(0)]
        public string OperationId { get; set; }
        
        /// <summary>
        /// Command creation time
        /// </summary>
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        /// <summary>
        /// Instrument
        /// </summary>
        [Key(3)]
        public string Instrument { get; set; }
        
        /// <summary>
        /// Position volume
        /// </summary>
        [Key(4)]
        public decimal Volume { get; set; }
        
        /// <summary>
        /// Streaming number of request. Increases in case when price arrived, but volume has changed. 
        /// </summary>
        [Key(5)]
        public int RequestNumber { get; set; }
    }
}