using System;
using System.Collections.Concurrent;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Core.Settings;
using Newtonsoft.Json;

namespace MarginTrading.Services.Infrastructure
{
    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly ILog _logger;
        private readonly IConsole _consoleWriter;

        private readonly ConcurrentBag<IStopable> _stopables = new ConcurrentBag<IStopable>();

        public RabbitMqService(ILog logger, IConsole consoleWriter)
        {
            _logger = logger;
            _consoleWriter = consoleWriter;
        }

        public void Dispose()
        {
            foreach (var stopable in _stopables)
                stopable.Stop();
        }

        public IMessageProducer<TMessage> CreateProducer<TMessage>(RabbitMqSettings settings)
        {
            var subscriptionSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.ConnectionString,
                ExchangeName = settings.ExchangeName,
                IsDurable = true
            };

            var rabbitMqPublisher = new RabbitMqPublisher<TMessage>(subscriptionSettings)
                .SetSerializer(new JsonMessageSerializer<TMessage>())
                .SetLogger(_logger)
                .SetConsole(_consoleWriter)
                .Start();
            
            _stopables.Add(rabbitMqPublisher);

            return rabbitMqPublisher;
        }

        public void Subscribe<TMessage>(RabbitMqSettings settings, string env, Func<TMessage, Task> handler)
        {
            var subscriptionSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.ConnectionString,
                QueueName = QueueHelper.BuildQueueName(settings.ExchangeName, env),
                ExchangeName = settings.ExchangeName,
                IsDurable = settings.IsDurable
            };

            var rabbitMqSubscriber = new RabbitMqSubscriber<TMessage>(subscriptionSettings,
                    new DefaultErrorHandlingStrategy(_logger, subscriptionSettings))
                .SetMessageDeserializer(new ErrorLoggingJsonMessageDeserializer<TMessage>(_logger))
                .Subscribe(handler)
                .SetLogger(_logger)
                .SetConsole(_consoleWriter)
                .Start();

            _stopables.Add(rabbitMqSubscriber);
        }
    }
}