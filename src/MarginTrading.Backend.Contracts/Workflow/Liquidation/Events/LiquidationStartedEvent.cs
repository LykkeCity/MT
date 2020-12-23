// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Workflow.Liquidation.Events
{
    [MessagePackObject]
    public class LiquidationStartedEvent
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }

        [Key(2)]
        public LiquidationTypeContract LiquidationType { get; set; }

        [Key(3)]
        public string AccountId { get; set; }

        [Key(4)]
        public string AssetPairId { get; set; }
    }
}