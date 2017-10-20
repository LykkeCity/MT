using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.MarketMaker.Settings
{
    public class RabbitConnectionSettings
    {
        public string ConnectionString { get; set; }
        public string ExchangeName { get; set; }
        [Optional, CanBeNull]
        public string AdditionalQueueSuffix { get; set; }
    }
}
