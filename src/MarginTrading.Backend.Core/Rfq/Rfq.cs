// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Rfq
{
    public class Rfq
    {
        public string Id { get; set; }
        public string InstrumentId { get; set; }
        public List<string> PositionIds { get; set; }
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public string ExternalProviderId { get; set; }
        public string AccountId { get; set; }
        public string CausationOperationId { get; set; }
        public string CreatedBy { get; set; }
        public OriginatorType OriginatorType { get; set; }
        public int RequestNumber { get; set; }
        public bool RequestedFromCorporateActions { get; set; }
        public SpecialLiquidationOperationState State { get; set; }
        public DateTime LastModified { get; set; }
        
        public RfqPauseSummary PauseSummary { get; set; }
    }
}