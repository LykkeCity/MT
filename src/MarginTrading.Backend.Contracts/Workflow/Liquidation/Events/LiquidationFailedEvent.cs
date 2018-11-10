using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Workflow.Liquidation.Events
{
    [MessagePackObject]
    public class LiquidationFailedEvent
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        [Key(2)]
        public string Reason { get; set; }
        
        [Key(3)]
        public LiquidationTypeContract LiquidationType { get; set; }
    }
}