namespace MarginTrading.Core
{
    public static class MarginTradingHelpers
    {
        // TODO: Rethink
        public const int VolumeAccuracy = 8;
    }

    public static class MatchingEngines
    {
        public const string Lykke = "LYKKE";
        public const string Icm = "ICM";

        public static string[] All = {Lykke, Icm};
    }
}
