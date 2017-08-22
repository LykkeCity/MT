using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.MarketMaker.Settings;
using Microsoft.Extensions.PlatformAbstractions;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly ILog _logger;
        private readonly ConcurrentBag<IStopable> _stopables = new ConcurrentBag<IStopable>();

        public RabbitMqService(ILog logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
            foreach (var stopable in _stopables)
                stopable.Stop();
        }

        public IMessageProducer<TMessage> CreateProducer<TMessage>(RabbitConnectionSettings settings, bool isDurable)
        {
            var subscriptionSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.ConnectionString,
                ExchangeName = settings.ExchangeName,
                IsDurable = true,
            };

            var rabbitMqPublisher = new RabbitMqPublisher<TMessage>(subscriptionSettings)
                .SetSerializer(new JsonMessageSerializer<TMessage>())
                .SetLogger(_logger)
                .Start();

            _stopables.Add(rabbitMqPublisher);

            return rabbitMqPublisher;
        }

        public void Subscribe<TMessage>(RabbitConnectionSettings settings, bool isDurable, Func<TMessage, Task> handler)
        {
            var subscriptionSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.ConnectionString,
                QueueName = $"{settings.ExchangeName}.{PlatformServices.Default.Application.ApplicationName}",
                ExchangeName = settings.ExchangeName,
                IsDurable = isDurable,
            };

            var rabbitMqSubscriber = new RabbitMqSubscriber<TMessage>(subscriptionSettings,
                    new DefaultErrorHandlingStrategy(_logger, subscriptionSettings))
                .SetMessageDeserializer(new JsonMessageDeserializer<TMessage>())
                .Subscribe(handler)
                .SetLogger(_logger)
                .Start();

            _stopables.Add(rabbitMqSubscriber);
        }
    }
}