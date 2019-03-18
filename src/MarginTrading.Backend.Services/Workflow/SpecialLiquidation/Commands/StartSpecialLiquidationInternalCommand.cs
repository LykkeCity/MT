using System;
using JetBrains.Annotations;
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
        
        [Key(2)]
        public string[] PositionIds { get; set; }
        
        [CanBeNull]
        [Key(3)]
        public string AccountId { get; set; }
        
        [CanBeNull]
        [Key(4)]
        public string CausationOperationId { get; set; }
        
        [Key(5)]
        public string AdditionalInfo { get; set; }
    }
}