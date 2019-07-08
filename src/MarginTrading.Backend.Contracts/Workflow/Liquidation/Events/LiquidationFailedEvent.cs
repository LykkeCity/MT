// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MarginTrading.Backend.Contracts.Positions;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Workflow.Liquidation.Events
{
    [MessagePackObject]
    public class LiquidationFailedEvent
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        [Key(2)]
        public string Reason { get; set; }
        
        [Key(3)]
        public LiquidationTypeContract LiquidationType { get; set; }
        
        [Key(4)]
        public string AccountId { get; set; }
        
        [Key(5)]
        public string AssetPairId { get; set; }
        
        [Key(6)]
        public PositionDirectionContract? Direction { get; set; }
        
        [Key(7)]
        public string QuoteInfo { get; set; }
        
        [Key(8)]
        public List<string> ProcessedPositionIds { get; set; }
        
        [Key(9)]
        public List<string> LiquidatedPositionIds { get; set; }
        
        [Key(10)]
        public int OpenPositionsRemainingOnAccount { get; set; }
        
        [Key(11)]
        public decimal CurrentTotalCapital { get; set; }
    }
}