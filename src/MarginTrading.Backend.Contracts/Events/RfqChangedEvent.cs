// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MarginTrading.Backend.Contracts.Rfq;

namespace MarginTrading.Backend.Contracts.Events
{
    /// <summary>
    /// The event published upon change of request for quote
    /// </summary>
    public class RfqChangedEvent
    {
        /// <summary>
        /// Request for quote operation id
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Positions list
        /// </summary>
        public List<string> PositionIds { get; set; }
        
        /// <summary>
        /// Volume
        /// </summary>
        public decimal Volume { get; set; }
        
        /// <summary>
        /// Price
        /// </summary>
        public decimal Price { get; set; }
        
        /// <summary>
        /// The sequential number number of request (when retrying)
        /// </summary>
        public int RequestNumber { get; set; }
        
        /// <summary>
        /// Operation state
        /// </summary>
        public RfqOperationState State { get; set; }
        
        /// <summary>
        /// Operation last modified timestamp
        /// </summary>
        public DateTime LastModified { get; set; }
        
        /// <summary>
        /// Account identifier
        /// </summary>
        public string AccountId { get; set; }
        
        /// <summary>
        /// Instrument identifier
        /// </summary>
        public string InstrumentId { get; set; }
        
        /// <summary>
        /// Pause summary information
        /// </summary>
        public RfqPauseSummaryChangedContract PauseSummary { get; set; }
    }
}