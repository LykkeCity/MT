using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    public class SpecialLiquidationSettings
    {
        public bool Enabled { get; set; }

        [Optional]
        public decimal FakePrice { get; set; } = 10;
    }
}