// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events
{
    /// <summary>
    /// Possible errors when resuming paused special liquidation
    /// </summary>
    public enum SpecialLiquidationResumePausedFailureReason
    {
        /// <summary>
        /// Error happened when acknowledging pause cancellation
        /// </summary>
        AcknowledgeError,
        
        /// <summary>
        /// There is no active pause to cancel
        /// </summary>
        NoActivePause
    }
}