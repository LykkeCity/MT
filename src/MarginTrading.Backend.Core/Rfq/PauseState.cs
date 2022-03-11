// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.Rfq
{
    /// <summary>
    /// The pause state used to pause special liquidation workflow 
    /// </summary>
    public enum PauseState
    {
        /// <summary>
        /// An intention to pause the workflow has been declared
        /// </summary>
        Pending = 0,
        
        /// <summary>
        /// The pause has been activated
        /// </summary>
        Active,
        
        /// <summary>
        /// An intention to resume the workflow has been declared
        /// </summary>
        PendingCancellation,
        
        /// <summary>
        /// The pause has been cancelled (workflow resumed)
        /// </summary>
        Cancelled
    }
}