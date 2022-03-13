// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using MarginTrading.Backend.Contracts.Rfq;

namespace MarginTrading.Backend.Core.Rfq
{
    public class RfqFilter
    {
        public string OperationId { get; set; }
        public string InstrumentId { get; set; }
        public string AccountId { get; set; }
        public IReadOnlyCollection<RfqOperationState> States { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}