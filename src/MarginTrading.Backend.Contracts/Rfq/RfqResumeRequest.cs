// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

namespace MarginTrading.Backend.Contracts.Rfq
{
    /// <summary>
    /// RFQ resume command request details
    /// </summary>
    public class RfqResumeRequest
    {
        /// <summary>
        /// The author of resume request
        /// </summary>
        [Required]
        public string Initiator { get; set; }
    }
}