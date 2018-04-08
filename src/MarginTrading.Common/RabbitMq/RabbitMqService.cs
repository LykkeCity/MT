using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AzureStorage.Blob;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.RabbitMq.Azure;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.SettingsReader;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;

namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly ILog _logger;
        private readonly string _env;
        private readonly IConsole _consoleWriter;

        private readonly ConcurrentDictionary<RabbitMqSubscriptionSettings, IStopable> _subscribers =
            new ConcurrentDictionary<RabbitMqSubscriptionSettings, IStopable>(new SubscriptionSettingsEqualityComparer());

        private readonly ConcurrentDictionary<RabbitMqSubscriptionSettings, Lazy<IStopable>> _producers =
            new ConcurrentDictionary<RabbitMqSubscriptionSettings, Lazy<IStopable>>(
                new SubscriptionSettingsEqualityComparer());

        [ItemCanBeNull] private readonly Lazy<MessagePackBlobPublishingQueueRepository> _queueRepository;

        public RabbitMqService(ILog logger, IConsole consoleWriter, 
            [CanBeNull] IReloadingManager<string> queueRepositoryConnectionString, string env)
        {
            _logger = logger;
            _env = env;
            _consoleWriter = consoleWriter;
            _queueRepository = new Lazy<MessagePackBlobPublishingQueueRepository>(() =>
            {
                if (string.IsNullOrWhiteSpace(queueRepositoryConnectionString?.CurrentValue))
                {
                    _logger.WriteWarning(nameof(RabbitMqService), "",
                        "QueueRepositoryConnectionString is not configured");
                    return null;
                }

                var blob = AzureBlobStorage.Create(queueRepositoryConnectionString);
                return new MessagePackBlobPublishingQueueRepository(blob);
            });
        }

        public void Dispose()
        {
            foreach (var stoppable in _subscribers.Values)
                stoppable.Stop();
            foreach (var stoppable in _producers.Values)
                stoppable.Value.Stop();
        }

        public IRabbitMqSerializer<TMessage> GetJsonSerializer<TMessage>()
        {
            return new JsonMessageSerializer<TMessage>();
        }

        public IRabbitMqSerializer<TMessage> GetMsgPackSerializer<TMessage>()
        {
            return new MessagePackMessageSerializer<TMessage>();
        }

        public IMessageDeserializer<TMessage> GetJsonDeserializer<TMessage>()
        {
            return new DeserializerWithErrorLogging<TMessage>(_logger);
        }

        public IMessageDeserializer<TMessage> GetMsgPackDeserializer<TMessage>()
        {
            return new MessagePackMessageDeserializer<TMessage>();
        }

        public IMessageProducer<TMessage> GetProducer<TMessage>(RabbitMqSettings settings,
            bool isDurable, IRabbitMqSerializer<TMessage> serializer)
        {
            // on-the fly connection strings switch is not supported currently for rabbitMq
            var subscriptionSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.ConnectionString,
                ExchangeName = settings.ExchangeName,
                IsDurable = isDurable,
            };

            return (IMessageProducer<TMessage>) _producers.GetOrAdd(subscriptionSettings, CreateProducer).Value;

            Lazy<IStopable> CreateProducer(RabbitMqSubscriptionSettings s)
            {
                // Lazy ensures RabbitMqPublisher will be created and started only once
                // https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
                return new Lazy<IStopable>(() =>
                {
                    var publisher = new RabbitMqPublisher<TMessage>(s);

                    if (isDurable && _queueRepository.Value != null)
                        publisher.SetQueueRepository(_queueRepository.Value);
                    else
                        publisher.DisableInMemoryQueuePersistence();

                    return publisher
                        .SetSerializer(serializer)
                        .SetLogger(_logger)
                        .SetConsole(_consoleWriter)
                        .Start();
                });
            }
        }

        public void Subscribe<TMessage>(RabbitMqSettings settings, bool isDurable,
            Func<TMessage, Task> handler, IMessageDeserializer<TMessage> deserializer)
        {
            var subscriptionSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.ConnectionString,
                QueueName = QueueHelper.BuildQueueName(settings.ExchangeName, _env),
                ExchangeName = settings.ExchangeName,
                IsDurable = isDurable,
            };

            var rabbitMqSubscriber = new RabbitMqSubscriber<TMessage>(subscriptionSettings,
                    new DefaultErrorHandlingStrategy(_logger, subscriptionSettings))
                .SetMessageDeserializer(deserializer)
                .Subscribe(handler)
                .SetLogger(_logger)
                .SetConsole(_consoleWriter);

            if (!_subscribers.TryAdd(subscriptionSettings, rabbitMqSubscriber))
            {
                throw new InvalidOperationException(
                    $"A subscriber for queue {subscriptionSettings.QueueName} was already initialized");
            }

            rabbitMqSubscriber.Start();
        }

        /// <remarks>
        ///     ReSharper auto-generated
        /// </remarks>
        private sealed class SubscriptionSettingsEqualityComparer : IEqualityComparer<RabbitMqSubscriptionSettings>
        {
            public bool Equals(RabbitMqSubscriptionSettings x, RabbitMqSubscriptionSettings y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.ConnectionString, y.ConnectionString) &&
                       string.Equals(x.ExchangeName, y.ExchangeName);
            }

            public int GetHashCode(RabbitMqSubscriptionSettings obj)
            {
                unchecked
                {
                    return ((obj.ConnectionString != null ? obj.ConnectionString.GetHashCode() : 0) * 397) ^
                           (obj.ExchangeName != null ? obj.ExchangeName.GetHashCode() : 0);
                }
            }
        }
    }
}