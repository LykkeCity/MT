// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.ErrorCodes
{
    /// <summary>
    /// The error codes to describe cases when uploading quotes for the trading
    /// day EOD was missed for 
    /// </summary>
    public enum QuotesUploadErrorCode
    {
        /// <summary>
        /// No error
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Invalid trading day was provided
        /// </summary>
        InvalidTradingDay,
        
        /// <summary>
        /// Empty list of fx and/or cfd quotes
        /// </summary>
        EmptyQuotes,
        
        /// <summary>
        /// There is no trading snapshot backup to apply quotes to
        /// </summary>
        NoDraft,
        
        /// <summary>
        /// There is another trading snapshot manipulation process running
        /// </summary>
        AlreadyInProgress
    }
}