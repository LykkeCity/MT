// Copyright (c) 2019 Lykke Corp.

using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Core.MarketMakerFeed
{
    /// <summary>
    /// Represents several commands to create or to delete orders for a specified <see cref="AssetPairId"/>
    /// </summary>
    public class MarketMakerOrderCommandsBatchMessage
    {
        /// <summary>
        /// Asset pair id
        /// </summary>
        public string AssetPairId { get; set; }

        /// <summary>
        /// Message generation timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Id of market maker which generated the command
        /// </summary>
        public string MarketMakerId { get; set; }

        /// <summary>
        /// Commands representing the batch
        /// </summary>
        public IReadOnlyList<MarketMakerOrderCommand> Commands { get; set; }
    }
}
