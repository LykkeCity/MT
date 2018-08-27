using System;
using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands
{
    [MessagePackObject]
    public class StartSpecialLiquidationInternalCommand
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
    }
}