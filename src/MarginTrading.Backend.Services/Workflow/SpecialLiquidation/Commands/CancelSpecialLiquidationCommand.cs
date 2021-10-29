// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands
{
    /// <summary>
    /// Cancelling special liquidation
    /// </summary>
    [MessagePackObject()]
    public class CancelSpecialLiquidationCommand
    {
        /// <summary>
        /// The operation id
        /// </summary>
        [Key(0)]
        public string OperationId { get; set; }
        
        /// <summary>
        /// The reason of cancelling
        /// </summary>
        [Key(1)]
        public string Reason { get; set; }
    }
}