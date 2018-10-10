using System;
using System.Collections.Generic;
using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.Liquidation.Events
{
    [MessagePackObject]
    public class PositionsLiquidationFinishedInternalEvent
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        [Key(2)]
        public List<LiquidationInfo> LiquidationInfos { get; set; }
        
        public PositionsLiquidationFinishedInternalEvent()
        {
            LiquidationInfos = new List<LiquidationInfo>();
        }
    }
    
    [MessagePackObject]
    public class LiquidationInfo
    {
        [Key(0)]
        public string PositionId {get; set; }
            
        [Key(1)]
        public bool IsLiquidated { get; set; }
            
        [Key(2)]
        public string Comment { get; set; }
    }
}