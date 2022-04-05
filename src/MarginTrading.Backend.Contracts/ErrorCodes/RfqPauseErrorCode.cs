// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.ErrorCodes
{
    /// <summary>
    /// The error codes to describe cases when RFQ pause may fail
    /// </summary>
    public enum RfqPauseErrorCode
    {
        /// <summary>
        /// No error
        /// </summary>
        None = 0,
        
        /// <summary>
        /// RFQ was not found
        /// </summary>
        NotFound,
        
        /// <summary>
        /// There is already pause object for the operation
        /// </summary>
        AlreadyExists,
        
        /// <summary>
        /// RFQ can not be paused due to business logic
        /// </summary>
        InvalidOperationState
    }
}