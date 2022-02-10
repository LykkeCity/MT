// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MarginTrading.Backend.Contracts.Common;

namespace MarginTrading.Backend.Contracts.Rfq
{
    /// <summary>
    /// RFQ resume command request details
    /// </summary>
    public class RfqResumeRequest
    {
        /// <summary>
        /// The reason for resume
        /// </summary>
        public string Reason { get; set; }
        
        /// <summary>
        /// The author of resume request
        /// </summary>
        [Required]
        public Initiator Initiator { get; set; }
    }
}