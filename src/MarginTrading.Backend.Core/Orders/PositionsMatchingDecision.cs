// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.Orders
{
    public sealed class PositionsMatchingDecision
    {
        /// <summary>
        /// The order decision is linked to
        /// </summary>
        public Order Order { get; }

        /// <summary>
        /// The order volume left to match
        /// </summary>
        public decimal VolumeToMatch {
            get
            {
                var oppositeDirectionVolume = Math.Abs(PositionsState?.Volume ?? 0);

                if (oppositeDirectionVolume >= Math.Abs(Order.Volume))
                {
                    return 0;
                }
                
                return Order.Volume >= 0
                    ? Order.Volume - oppositeDirectionVolume
                    : Order.Volume + oppositeDirectionVolume;
            } 
        }

        /// <summary>
        /// Designates if new position will be opened optionally taking into account the possibility of closing opposite
        /// direction positions to fulfill or partially fulfill the order
        /// </summary>
        public bool ShouldOpenPosition { get; }

        /// <summary>
        /// The timestamp of the decision
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Opposite direction matched positions state
        /// </summary>
        [CanBeNull]
        public MatchedPositionsState PositionsState { get; }

        private PositionsMatchingDecision(Order order,
            bool shouldOpenPosition,
            DateTime timestamp)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
            ShouldOpenPosition = shouldOpenPosition;
            Timestamp = timestamp;
        }

        private PositionsMatchingDecision(Order order,
            DateTime timestamp,
            MatchedPositionsState positionsState)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
            Timestamp = timestamp;
            PositionsState = positionsState;

            ShouldOpenPosition = Math.Abs(VolumeToMatch) > 0;
        }

        /// <summary>
        /// Creates the forced decision regardless of any other conditions
        /// </summary>
        /// <param name="order">The order</param>
        /// <param name="timestamp">The timestamp of the state</param>
        /// <param name="shouldOpenPosition">Indicates if new position should be opened or not</param>
        /// <returns></returns>
        public static PositionsMatchingDecision Force(Order order, DateTime timestamp, bool shouldOpenPosition) =>
            new PositionsMatchingDecision(order, shouldOpenPosition, timestamp);

        /// <summary>
        /// Creates the decision to open new position based on matched volume math
        /// </summary>
        /// <param name="order">The order</param>
        /// <param name="timestamp">The timestamp of the state</param>
        /// <param name="positionsState">The opposite direction matched positions state</param>
        /// <returns></returns>
        public static PositionsMatchingDecision Create(Order order,
            DateTime timestamp,
            MatchedPositionsState positionsState) =>
            new PositionsMatchingDecision(order, timestamp, positionsState);
    }
}