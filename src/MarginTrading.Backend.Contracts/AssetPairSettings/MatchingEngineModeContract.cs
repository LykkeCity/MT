﻿using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.AssetPairSettings
{
    [PublicAPI]
    public enum MatchingEngineModeContract
    {
        /// <summary>
        /// Market making mode with actual matching on our side
        /// </summary>
        MarketMaker = 1,

        /// <summary>
        /// Straight through processing with orders matching on external exchanges
        /// </summary>
        Stp = 2,
    }
}