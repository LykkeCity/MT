// Copyright (c) 2021 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Contracts.Rfq
{
    /// <summary>
    /// Request for quote contract
    /// </summary>
    public class RfqContract
    {
        /// <summary>
        /// Operation identifier
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Instrument identifier
        /// </summary>
        public string InstrumentId { get; set; }
        
        /// <summary>
        /// The list of positions
        /// </summary>
        public List<string> PositionIds { get; set; }
        
        /// <summary>
        /// The volume
        /// </summary>
        public decimal Volume { get; set; }
        
        /// <summary>
        /// The price received
        /// </summary>
        public decimal Price { get; set; }
        
        /// <summary>
        /// The external provider identifier
        /// </summary>
        public string ExternalProviderId { get; set; }
        
        /// <summary>
        /// The account identifier
        /// </summary>
        public string AccountId { get; set; }
        
        /// <summary>
        /// The operation identifier which caused the RFQ
        /// </summary>
        public string CausationOperationId { get; set; }
        
        /// <summary>
        /// The initiator of RFQ
        /// </summary>
        public string CreatedBy { get; set; }
        
        /// <summary>
        /// The type of initiator
        /// </summary>
        public RfqOriginatorType OriginatorType { get; set; }
        
        /// <summary>
        /// The sequential number number of request (when retrying)
        /// </summary>
        public int RequestNumber { get; set; }
        
        /// <summary>
        /// If RFQ was initiated by corporate action
        /// </summary>
        public bool RequestedFromCorporateActions { get; set; }
        
        /// <summary>
        /// RFQ state
        /// </summary>
        public RfqOperationState State { get; set; }
        
        /// <summary>
        /// Last modified timestamp
        /// </summary>
        public DateTime LastModified { get; set; }
        
        /// <summary>
        /// RFQ pause summary contract
        /// </summary>
        public RfqPauseSummaryContract Pause { get; set; }
    }
}