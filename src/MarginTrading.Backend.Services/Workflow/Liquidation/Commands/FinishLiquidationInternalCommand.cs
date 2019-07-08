// Copyright (c) 2019 Lykke Corp.

using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core;
using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.Liquidation.Commands
{
    [MessagePackObject]
    public class FinishLiquidationInternalCommand
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        [Key(2)]
        public string Reason { get; set; }
        
        [Key(3)]
        public LiquidationType LiquidationType { get; set; }
        
        [Key(4)]
        public List<string> ProcessedPositionIds { get; set; }
        
        [Key(5)]
        public List<string> LiquidatedPositionIds { get; set; }
    }
}