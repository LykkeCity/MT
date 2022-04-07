// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.Rfq;

namespace MarginTrading.Backend.Contracts.Events
{
    /// <summary>
    /// The event published upon change of request for quote
    /// </summary>
    public sealed class RfqEvent
    {
        /// <summary>
        /// The RFQ snapshot
        /// </summary>
        public RfqContract RfqSnapshot { get; set; }
        
        /// <summary>
        /// The event type
        /// </summary>
        public RfqEventTypeContract EventType { get; set; }
    }
}