// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Orders
{
    /// <summary>
    /// Order matched positions state
    /// </summary>
    public sealed class MatchedPositionsState
    {
        /// <summary>
        /// The order identifier state is linked to
        /// </summary>
        public string OrderId { get; }
        

        /// <summary>
        /// All matching positions margin
        /// </summary>
        public decimal Margin { get; }

        /// <summary>
        /// All matching positions volume
        /// </summary>
        public decimal Volume { get; }

        public MatchedPositionsState(string orderId,
            decimal margin,
            decimal volume)
        {
            OrderId = orderId;
            Margin = margin;
            Volume = volume;
        }
    }
}