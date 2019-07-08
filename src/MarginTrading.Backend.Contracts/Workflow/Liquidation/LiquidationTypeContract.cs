// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.Workflow.Liquidation
{
    public enum LiquidationTypeContract
    {
        /// <summary>
        /// Stop out caused liquidation
        /// </summary>
        Normal = 0,
        /// <summary>
        /// MCO caused liquidation
        /// </summary>
        Mco = 1,
        /// <summary>
        /// Liquidation is started by "Close All" API call
        /// </summary>
        Forced = 2,
    }
}