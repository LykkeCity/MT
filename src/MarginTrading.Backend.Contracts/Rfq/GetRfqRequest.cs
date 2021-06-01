// Copyright (c) 2021 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Contracts.Rfq
{
    public class GetRfqRequest
    {
        /// <summary>
        /// Exact match
        /// </summary>
        public string RfqId { get; set; }

        /// <summary>
        /// Exact match
        /// </summary>
        public string InstrumentId { get; set; }

        /// <summary>
        /// Investor account.
        /// Exact match.
        /// </summary>
        public string AccountId { get; set; }

        public IReadOnlyCollection<RfqOperationState> States { get; set; }

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}