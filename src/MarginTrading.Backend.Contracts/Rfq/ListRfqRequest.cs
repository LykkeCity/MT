// Copyright (c) 2021 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Contracts.Rfq
{
    /// <summary>
    /// Details of RFQ list request
    /// </summary>
    public class ListRfqRequest
    {
        /// <summary>
        /// Operation id exact match
        /// </summary>
        public string RfqId { get; set; }

        /// <summary>
        /// Instrument id exact match
        /// </summary>
        public string InstrumentId { get; set; }

        /// <summary>
        /// Investor account id.
        /// Exact match.
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// The list of states RFQ is in.
        /// </summary>
        public IReadOnlyCollection<RfqOperationState> States { get; set; }

        /// <summary>
        /// Applies to RFQ LastModified timestamp including dateFrom date 
        /// </summary>
        public DateTime? DateFrom { get; set; }
        
        /// <summary>
        /// Applies to RFQ LastModified timestamp excluding dateTo date
        /// </summary>
        public DateTime? DateTo { get; set; }
        
        /// <summary>
        /// Denotes if RFQ can be paused
        /// </summary>
        public bool? CanBePaused { get; set; }
        
        /// <summary>
        /// Denotes if paused RFQ can be resumed 
        /// </summary>
        public bool? CanBeResumed { get; set; }
    }
}