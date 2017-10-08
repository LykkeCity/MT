namespace MarginTrading.Core
{
    public static class MarginTradingHelpers
    {
        // TODO: need to use different accuracy for different asset pairs
        public const int VolumeAccuracy = 8;
        // TODO: need to use different accuracy for different assets
        public const int DefaultAssetAccuracy = 8;
    }

    public static class MatchingEngines
    {
        public const string Lykke = "LYKKE";
        public const string Icm = "ICM";

        public static string[] All = {Lykke, Icm};
    }
}
