// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;

namespace MarginTrading.Common.RabbitMq
{
    public interface IRabbitMqService
    {
        IMessageProducer<TMessage> GetProducer<TMessage>(RabbitMqSettings settings, bool isDurable,
            IRabbitMqSerializer<TMessage> serializer);

        void Subscribe<TMessage>(RabbitMqSettings settings, bool isDurable, Func<TMessage, Task> handler,
            IMessageDeserializer<TMessage> deserializer);

        IRabbitMqSerializer<TMessage> GetJsonSerializer<TMessage>();
        IRabbitMqSerializer<TMessage> GetMsgPackSerializer<TMessage>();

        IMessageDeserializer<TMessage> GetJsonDeserializer<TMessage>();
        IMessageDeserializer<TMessage> GetMsgPackDeserializer<TMessage>();
    }
}