// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.ErrorCodes
{
    /// <summary>
    /// The error codes to describe cases when RFQ resume may fail
    /// </summary>
    public enum RfqResumeErrorCode
    {
        /// <summary>
        /// No error
        /// </summary>
        None = 0,
        
        /// <summary>
        /// The RFQ was not found
        /// </summary>
        NotFound,
        
        /// <summary>
        /// The RFQ is not on pause
        /// </summary>
        NotPaused,
        
        /// <summary>
        /// Database issue
        /// </summary>
        Persistence
    }
}