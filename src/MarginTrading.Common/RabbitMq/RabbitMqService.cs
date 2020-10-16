// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
using RabbitMQ.Client;

namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly ILog _logger;
        private readonly string _env;
        private readonly IPublishingQueueRepository _publishingQueueRepository;
        private readonly IConsole _consoleWriter;

        private readonly ConcurrentDictionary<(RabbitMqSubscriptionSettings, int), IStopable> _subscribers =
            new ConcurrentDictionary<(RabbitMqSubscriptionSettings, int), IStopable>(new SubscriptionSettingsWithNumberEqualityComparer());

        private readonly ConcurrentDictionary<RabbitMqSubscriptionSettings, Lazy<IStopable>> _producers =
            new ConcurrentDictionary<RabbitMqSubscriptionSettings, Lazy<IStopable>>(
                new SubscriptionSettingsEqualityComparer());

        //[ItemCanBeNull] private readonly Lazy<MessagePackBlobPublishingQueueRepository> _queueRepository;

        public RabbitMqService(ILog logger, 
            IConsole consoleWriter, 
            [CanBeNull] IReloadingManager<string> queueRepositoryConnectionString, 
            string env,
            IPublishingQueueRepository publishingQueueRepository)
        {
            _logger = logger;
            _env = env;
            _publishingQueueRepository = publishingQueueRepository;
            _consoleWriter = consoleWriter;
            //_queueRepository = new Lazy<MessagePackBlobPublishingQueueRepository>(() =>
//            {
//                if (string.IsNullOrWhiteSpace(queueRepositoryConnectionString?.CurrentValue))
//                {
//                    _logger.WriteWarning(nameof(RabbitMqService), "",
//                        "QueueRepositoryConnectionString is not configured");
//                    return null;
//                }

                //var blob = AzureBlobStorage.Create(queueRepositoryConnectionString);
                //return new MessagePackBlobPublishingQueueRepository(blob);
            //});
        }

        /// <summary>
        /// Returns the number of messages in <paramref name="queueName"/> ready to be delivered to consumers.
        /// This method assumes the queue exists. If it doesn't, an exception is thrown.
        /// </summary>
        public static uint GetMessageCount(string connectionString, string queueName)
        {
            var factory = new ConnectionFactory { Uri = new Uri(connectionString, UriKind.Absolute) };
            
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                return channel.MessageCount(queueName);
            }
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
            IRabbitMqSerializer<TMessage> serializer)
        {
            // on-the fly connection strings switch is not supported currently for rabbitMq
            var subscriptionSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.ConnectionString,
                ExchangeName = settings.ExchangeName,
                IsDurable = settings.IsDurable,
            };

            return (IMessageProducer<TMessage>) _producers.GetOrAdd(subscriptionSettings, CreateProducer).Value;

            Lazy<IStopable> CreateProducer(RabbitMqSubscriptionSettings s)
            {
                // Lazy ensures RabbitMqPublisher will be created and started only once
                // https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
                return new Lazy<IStopable>(() =>
                {
                    var publisher = new RabbitMqPublisher<TMessage>(s);

                    if (s.IsDurable && _publishingQueueRepository != null)
                        publisher.SetQueueRepository(_publishingQueueRepository);
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
            var consumerCount = settings.ConsumerCount == 0 ? 1 : settings.ConsumerCount;
            
            foreach (var consumerNumber in Enumerable.Range(1, consumerCount))
            {
                var subscriptionSettings = new RabbitMqSubscriptionSettings
                {
                    ConnectionString = settings.ConnectionString,
                    QueueName = QueueHelper.BuildQueueName(settings.ExchangeName, _env),
                    ExchangeName = settings.ExchangeName,
                    IsDurable = isDurable,
                    RoutingKey = settings.RoutingKey,
                };
                
                var rabbitMqSubscriber = new RabbitMqSubscriber<TMessage>(subscriptionSettings,
                        new DefaultErrorHandlingStrategy(_logger, subscriptionSettings))
                    .SetMessageDeserializer(deserializer)
                    .Subscribe(handler)
                    .SetLogger(_logger)
                    .SetConsole(_consoleWriter);

                if (!_subscribers.TryAdd((subscriptionSettings, consumerNumber), rabbitMqSubscriber))
                {
                    throw new InvalidOperationException(
                        $"A subscriber number {consumerNumber} for queue {subscriptionSettings.QueueName} was already initialized");
                }

                rabbitMqSubscriber.Start();
            }
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

        /// <remarks>
        ///     ReSharper auto-generated
        /// </remarks>
        private sealed class SubscriptionSettingsWithNumberEqualityComparer : IEqualityComparer<(RabbitMqSubscriptionSettings, int)>
        {
            public bool Equals((RabbitMqSubscriptionSettings, int) x, (RabbitMqSubscriptionSettings, int) y)
            {
                if (ReferenceEquals(x.Item1, y.Item1) && x.Item2 == y.Item2) return true;
                if (ReferenceEquals(x.Item1, null)) return false;
                if (ReferenceEquals(y.Item1, null)) return false;
                if (x.Item1.GetType() != y.Item1.GetType()) return false;
                return string.Equals(x.Item1.ConnectionString, y.Item1.ConnectionString)
                       && string.Equals(x.Item1.ExchangeName, y.Item1.ExchangeName)
                       && x.Item2 == y.Item2;
            }

            public int GetHashCode((RabbitMqSubscriptionSettings, int) obj)
            {
                unchecked
                {
                    return ((obj.Item1.ConnectionString != null ? obj.Item1.ConnectionString.GetHashCode() : 0) * 397) ^
                           (obj.Item1.ExchangeName != null ? obj.Item1.ExchangeName.GetHashCode() : 0);
                }
            }
        }
    }
}