using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }
        public string ExchangeName { get; set; }
        [Optional]
        //By default = 1
        public int ConsumerCount { get; set; }
    }
}