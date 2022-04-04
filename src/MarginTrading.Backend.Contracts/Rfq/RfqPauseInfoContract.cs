// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Contracts.Rfq
{
    /// <summary>
    /// RFQ current pause information
    /// </summary>
    public class RfqPauseInfoContract
    {
        /// <summary>
        /// The state of pause
        /// </summary>
        public string State { get; set; }
        
        /// <summary>
        /// The pause source
        /// </summary>
        public string Source { get; set; }
        
        /// <summary>
        /// The pause timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Current pause effective since
        /// </summary>
        public DateTime? EffectiveSince { get; set; }
        
        /// <summary>
        /// Current pause initiator
        /// </summary>
        public string Initiator { get; set; }
    }
}