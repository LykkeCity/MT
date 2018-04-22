using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqSettings
    {
        [AmqpCheck(false)]
        public string ConnectionString { get; set; }
        public string ExchangeName { get; set; }
    }
}