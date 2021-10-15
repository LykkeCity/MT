// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;

namespace MarginTrading.Common.RabbitMq
{
    public class DeserializerWithErrorLogging<TMessage> : IMessageDeserializer<TMessage>
    {
        private readonly ILog _log;
        private readonly IMessageDeserializer<TMessage> _baseDeserializer;

        public DeserializerWithErrorLogging(ILog log, IMessageDeserializer<TMessage> baseDeserializer = null)
        {
            _log = log;
            _baseDeserializer =
                baseDeserializer ?? new JsonMessageDeserializer<TMessage>();
        }

        public TMessage Deserialize(byte[] data)
        {
            try
            {
                return _baseDeserializer.Deserialize(data);
            }
            catch (Exception e)
            {
                var rawObject = Encoding.UTF8.GetString(data);
                _log.WriteWarningAsync("RabbitMqSubscriber", "Deserialization", typeof(TMessage).FullName,
                    $"Deserializing: {e.Message}. {Environment.NewLine}Raw message: [{rawObject}]");
                throw;
            }
        }
    }
}