// Copyright (c) 2019 Lykke Corp.

using System.Text;
using Lykke.RabbitMqBroker.Publisher;

namespace MarginTrading.Common.RabbitMq
{
    public class BytesStringSerializer : IRabbitMqSerializer<string>
    {
        public byte[] Serialize(string model)
        {
            return Encoding.UTF8.GetBytes(model);
        }
    }
}
