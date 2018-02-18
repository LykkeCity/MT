using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }
        public string ExchangeName { get; set; }
        public bool IsDurable { get; set; }
    }
}