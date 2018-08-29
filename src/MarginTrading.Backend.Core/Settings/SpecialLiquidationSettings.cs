using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    public class SpecialLiquidationSettings
    {
        public bool Enabled { get; set; }

        [Optional]
        public decimal FakePrice { get; set; } = 10;

        [Optional]
        public int PriceRequestTimeoutSec { get; set; } = 3600;
    }
}