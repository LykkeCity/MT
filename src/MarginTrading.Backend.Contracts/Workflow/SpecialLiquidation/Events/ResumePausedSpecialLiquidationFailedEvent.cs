// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MessagePack;

namespace MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events
{
    /// <summary>
    /// An attempt to resume paused special liquidation failed
    /// </summary>
    [MessagePackObject]
    public class ResumePausedSpecialLiquidationFailedEvent
    {
        /// <summary>
        /// Operation Id
        /// </summary>
        [Key(0)]
        public string OperationId { get; set; }
        
        /// <summary>
        /// The reason of failure to resume paused special liquidation
        /// </summary>
        [Key(1)]
        public string Reason { get; set; }
    }
}