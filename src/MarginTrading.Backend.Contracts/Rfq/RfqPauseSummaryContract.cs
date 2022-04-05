// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.Rfq
{
    public class RfqPauseSummaryContract
    {
        /// <summary>
        /// Flag, if RFQ is paused on price request retry
        /// </summary>
        public bool IsPaused { get; set; }
        
        /// <summary>
        /// The pause source
        /// </summary>
        public string PauseReason { get; set; }
        
        /// <summary>
        /// The resume source if RFQ pause was cancelled
        /// </summary>
        public string ResumeReason { get; set; }
        
        /// <summary>
        /// Flag, if RFQ can be paused
        /// </summary>
        public bool CanBePaused { get; set; }
        
        /// <summary>
        /// Flag, if RFQ can be resumed
        /// </summary>
        public bool CanBeResumed { get; set; }
    }
}