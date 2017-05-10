using Lykke.RabbitMqBroker.Publisher;
using RabbitMQ.Client;

namespace MarginTrading.Common.RabbitMq
{
    public class TopicPublishStrategy : IRabbitMqPublishStrategy
    {
        private readonly string _routingKey;

        public TopicPublishStrategy(string routingKey = "")
        {
            _routingKey = routingKey;
        }

        public void Configure(RabbitMqPublisherSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(settings.ExchangeName, ExchangeType.Topic, true);
        }

        public void Publish(RabbitMqPublisherSettings settings, IModel channel, byte[] body)
        {
            channel.BasicPublish(settings.ExchangeName, _routingKey, null, body);
        }
    }
}
