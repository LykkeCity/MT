using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.MarketMaker.Settings
{
    public class AppSettings
    {
        public DbSettings Db { get; set; }

        public RabbitMqSettings RabbitMq { get; set; }

        [CanBeNull, Optional]
        public SlackNotificationsSettings SlackNotifications { get; set; }

        [CanBeNull, Optional]
        public string ApplicationInsightsKey { get; set; }
    }
}
