// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
    public class LiquidationOperationData : OperationDataBase<LiquidationOperationState>
    {
        public DateTime StartedAt { get; set; }
        public string AccountId { get; set; }
        public string AssetPairId { get; set; }
        public PositionDirection? Direction { get; set; }
        public string QuoteInfo { get; set; }
        public List<string> ProcessedPositionIds { get; set; }
        public List<string> LiquidatedPositionIds { get; set; }
        public LiquidationType LiquidationType { get; set; }
        public OriginatorType OriginatorType { get; set; }
        public string AdditionalInfo { get; set; }
    }
}