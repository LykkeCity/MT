// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Positions
{
    /// <summary>
    /// The direction of a position
    /// </summary>
    [PublicAPI]
    public enum PositionDirectionContract
    {
        /// <summary>
        /// Position is profitable if the price goes up 
        /// </summary>
        Long = 1,
        
        /// <summary>
        /// Position is profitable if the price goes down
        /// </summary>
        Short = 2
    }
}