using System;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
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
        
        [Key(2)]
        public string AccountId { get; set; }
        
        [Key(3)]
        public string AssetPairId { get; set; }
        
        [Key(4)]
        public PositionDirection? Direction { get; set; }
        
        [Key(5)]
        public string QuoteInfo { get; set; }
        
        [Key(6)]
        public LiquidationType LiquidationType { get; set; }
        
        [Key(7)]
        public OriginatorType OriginatorType { get; set; }
    }
}