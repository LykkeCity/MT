// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.MatchingEngines;

namespace MarginTrading.Backend.Core
{
    public static class MarginTradingHelpers
    {
        // TODO: need to use different accuracy for different asset pairs
        public const int VolumeAccuracy = 10;
    }

    public static class MatchingEngineConstants
    {
        public const string Reject = "REJECT";
        public const string DefaultMm = "MM";
        public const string LykkeCyStp = "LYKKECY_STP";
        public const string DefaultStp = "STP";
        public const string DefaultSpecialLiquidation = "SPECIAL_LIQUIDATION";
    }

    public static class AssetsConstants
    {
        public const int DefaultAssetAccuracy = 2;
    }

    public static class AssetPairConstants
    {
        public const int Accuracy = 5;
        public const string BasePairId = null;
        public const MatchingEngineMode MatchingEngineMode = (MatchingEngineMode)2;
        public const decimal StpMultiplierMarkupBid = 1;
        public const decimal StpMultiplierMarkupAsk = 1;
        public const string BaseCurrencyId = "EUR";
        public const string FxMarketId = "FxMarket";
    }
}
