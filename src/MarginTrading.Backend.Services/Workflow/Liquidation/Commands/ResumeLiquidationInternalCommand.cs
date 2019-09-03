// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
        
        [Key(5)]
        public List<string> PositionsLiquidatedBySpecialLiquidation { get; set; }
        
        [Key(6)]
        public bool ResumeOnlyFailed { get; set; }
    }
}