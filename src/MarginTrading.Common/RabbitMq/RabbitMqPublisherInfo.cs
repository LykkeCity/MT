// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqPublisherInfo
    {
        public string ExchangeName { get; set; }

        public RabbitMqSettings ToRabbitMqSettings(string connectionString)
        {
            return  new RabbitMqSettings
            {
                ConnectionString = connectionString,
                ExchangeName = ExchangeName,
            };
        }
    }
}