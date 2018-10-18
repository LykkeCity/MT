using System;
using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.Liquidation.Commands
{
    [MessagePackObject]
    public class ResumeLiquidationInternalCommand
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        [Key(2)]
        public string Comment { get; set; }
        
        [Key(3)]
        public bool IsCausedBySpecialLiquidation { get; set; }
        
        [Key(4)]
        public string CausationOperationId { get; set; }
    }
}