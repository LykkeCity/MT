// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;

namespace MarginTrading.Common.RabbitMq
{
    public interface IRabbitMqService
    {
        IMessageProducer<TMessage> GetProducer<TMessage>(RabbitMqPublisherConfiguration configuration,
            IRabbitMqSerializer<TMessage> serializer);

        void Subscribe<TMessage>(RabbitMqConsumerConfiguration configuration, bool isDurable, Func<TMessage, Task> handler,
            IMessageDeserializer<TMessage> deserializer);

        IRabbitMqSerializer<TMessage> GetJsonSerializer<TMessage>();
        IRabbitMqSerializer<TMessage> GetMsgPackSerializer<TMessage>();

        IMessageDeserializer<TMessage> GetJsonDeserializer<TMessage>();
        IMessageDeserializer<TMessage> GetMsgPackDeserializer<TMessage>();
    }
}