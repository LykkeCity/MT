using System;
using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.Liquidation.Commands
{
    [MessagePackObject]
    public class StartLiquidationInternalCommand
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        [Key(3)]
        public string[] PositionIds { get; set; }
    }
}