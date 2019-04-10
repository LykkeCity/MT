using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }
        public string ExchangeName { get; set; }
        
        /// By default = 1
        [Optional]
        public int ConsumerCount { get; set; } = 1;
    }
}