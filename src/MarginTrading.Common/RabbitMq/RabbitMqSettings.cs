// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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

        [Optional]
        public bool IsDurable { get; set; } = true;

        [Optional]
        public string RoutingKey { get; set; }
    }
}