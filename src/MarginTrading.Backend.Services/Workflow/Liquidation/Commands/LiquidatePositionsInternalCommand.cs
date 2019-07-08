// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core.Orders;
using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.Liquidation.Commands
{
    [MessagePackObject]
    public class LiquidatePositionsInternalCommand
    {
        [Key(0)] public string OperationId { get; set; }

        [Key(1)] public DateTime CreationTime { get; set; }

        [Key(2)] public string AssetPairId { get; set; }

        [Key(3)] public string[] PositionIds { get; set; }
    }
}