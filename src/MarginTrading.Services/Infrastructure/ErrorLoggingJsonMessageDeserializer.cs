using System;
using System.Text;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;

namespace MarginTrading.Services.Infrastructure
{
    public class ErrorLoggingJsonMessageDeserializer<TMessage> : IMessageDeserializer<TMessage>
    {
        private readonly ILog _log;
        private readonly IMessageDeserializer<TMessage> _jsonDeserializer;

        public ErrorLoggingJsonMessageDeserializer(ILog log)
        {
            _log = log;
            _jsonDeserializer = new JsonMessageDeserializer<TMessage>();
        }
        
        public TMessage Deserialize(byte[] data)
        {
            try
            {
                return _jsonDeserializer.Deserialize(data);
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