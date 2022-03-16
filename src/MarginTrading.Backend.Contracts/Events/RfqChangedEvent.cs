// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MarginTrading.Backend.Contracts.Rfq;

namespace MarginTrading.Backend.Contracts.Events
{
    public class RfqChangedEvent
    {
        public string Id { get; set; }
        public List<string> PositionIds { get; set; }
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public int RequestNumber { get; set; }
        public RfqOperationState State { get; set; }
        public DateTime LastModified { get; set; }
        
        public RfqPauseSummaryChangedContract PauseSummary { get; set; }
    }
}