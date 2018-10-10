using System;
using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.Liquidation.Events
{
    [MessagePackObject]
    public class NotEnoughLiquidityInternalEvent
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        [Key(2)]
        public string[] PositionIds { get; set; }
        
        [Key(3)]
        public string AdditionalInfo { get; set; }
    }
}