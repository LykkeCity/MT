using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.MarketMaker.Settings
{
    public class MarginTradingMarketMakerSettings
    {
        public DbSettings Db { get; set; }

        public RabbitMqSettings RabbitMq { get; set; }

        public string MarketMakerId { get; set; }

        [CanBeNull, Optional]
        public string ApplicationInsightsKey { get; set; }
    }
}
