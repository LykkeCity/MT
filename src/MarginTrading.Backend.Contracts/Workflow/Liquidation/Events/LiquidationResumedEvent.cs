// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Workflow.Liquidation.Events
{
    [MessagePackObject]
    public class LiquidationResumedEvent
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        [Key(2)]
        public bool IsCausedBySpecialLiquidation { get; set; }
        
        [Key(3)]
        public string Comment { get; set; }
        
        [Key(4)]
        public List<string> PositionsLiquidatedBySpecialLiquidation { get; set; }

        [Key(5)]
        public LiquidationTypeContract LiquidationType { get; set; }

        [Key(6)]
        public string AccountId { get; set; }

        [Key(7)]
        public string AssetPairId { get; set; }
    }
}