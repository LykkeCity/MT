using System;
using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Events
{
    [MessagePackObject]
    public class SpecialLiquidationStartedInternalEvent
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
    }
}