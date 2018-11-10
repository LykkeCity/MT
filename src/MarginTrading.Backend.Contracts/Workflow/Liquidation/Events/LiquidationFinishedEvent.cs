using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Workflow.Liquidation.Events
{
    [MessagePackObject]
    public class LiquidationFinishedEvent
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        [Key(3)]
        public LiquidationTypeContract LiquidationType { get; set; }
    }
}