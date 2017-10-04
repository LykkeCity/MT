using System;

namespace MarginTrading.Core
{
    public static class LykkeConstants
    {
        public const string BitcoinAssetId = "BTC";
        public const string LykkeAssetId = "LKK";

        public const string UsdAssetId = "USD";
        public const string EurAssetId = "EUR";
        public const string ChfAssetId = "CHF";
        public const string GbpAssetId = "GBP";
        public const string EthAssetId = "ETH";
        public const string SolarAssetId = "SLR";
        public const string ChronoBankAssetId = "TIME";

        public const string LKKUSDPairId = "LKKUSD";

        public const int TotalLykkeAmount = 1250000000;

        public const int MinPwdLength = 6;
        public const int MaxPwdLength = 100;

        public const int DefaultRefundTimeoutDays = 30;

        public static readonly string[] BaseAssets = { "USD", "CHF", "EUR" };
        public static decimal DefaultDemoBalance = 50000;
        public const decimal DefaultMarginCall = 0.8M;
        public const decimal DefaultStopOut = 0.95M;

        public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(3);
        public static readonly TimeSpan SessionRefreshPeriod = TimeSpan.FromDays(1);

        public const string StateBlobContainer = "state";

        public const string MaintenanceModeRoute = "service/maintenance";
    }
}