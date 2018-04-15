namespace MarginTrading.Backend.Core
{
    public static class LykkeConstants
    {
        public const string BitcoinAssetId = "BTC";
        public const string LykkeAssetId = "LKK";
        public const string EthAssetId = "ETH";
        public const string SolarAssetId = "SLR";

        public const decimal DefaultDemoBalance = 50000;
        public const decimal DefaultMarginCall = 0.8M;
        public const decimal DefaultStopOut = 0.95M;

        public const string StateBlobContainer = "state";
        public const string MigrationsBlobContainer = "migrations";

        public const string MaintenanceModeRoute = "maintenance";
    }
}