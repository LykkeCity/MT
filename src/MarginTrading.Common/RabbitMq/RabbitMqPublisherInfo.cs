// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqPublisherInfo
    {
        public string ExchangeName { get; set; }

        public RabbitMqSettings ToRabbitMqSettings(string connectionString)
        {
            var result = new RabbitMqSettings
            {
                ConnectionString = connectionString,
                ExchangeName = ExchangeName,
            };
            
            if (IsDurable.HasValue)
                result.IsDurable = IsDurable.Value;
            
            return result;
        }
        
        [Optional]
        public bool? IsDurable { get; set; }
    }
}