// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;
using Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling;
using Lykke.Snow.Common.Correlation.RabbitMq;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILog _logger;
        private readonly string _env;
        private readonly IPublishingQueueRepository _publishingQueueRepository;
        private readonly RabbitMqCorrelationManager _correlationManager;

        private readonly ConcurrentDictionary<(RabbitMqSubscriptionSettings, int), IStartStop> _subscribers =
            new ConcurrentDictionary<(RabbitMqSubscriptionSettings, int), IStartStop>(new SubscriptionSettingsWithNumberEqualityComparer());

        private readonly ConcurrentDictionary<RabbitMqSubscriptionSettings, Lazy<IStartStop>> _producers =
            new ConcurrentDictionary<RabbitMqSubscriptionSettings, Lazy<IStartStop>>(
                new SubscriptionSettingsEqualityComparer());

        private const short QueueNotFoundErrorCode = 404;

        public RabbitMqService(
            ILoggerFactory loggerFactory,
            ILog logger,
            string env,
            IPublishingQueueRepository publishingQueueRepository,
            RabbitMqCorrelationManager correlationManager)
        {
            _loggerFactory = loggerFactory;
            _logger = logger;
            _env = env;
            _publishingQueueRepository = publishingQueueRepository;
            _correlationManager = correlationManager;
        }

        /// <summary>
        /// Returns the number of messages in <paramref name="queueName"/> ready to be delivered to consumers.
        /// This method assumes the queue exists. If it doesn't, an exception is thrown.
        /// </summary>
        public static uint GetMessageCount(string connectionString, string queueName)
        {
            var factory = new ConnectionFactory { Uri = new Uri(connectionString, UriKind.Absolute) };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            try
            {
                return channel.QueueDeclarePassive(queueName).MessageCount;
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyCode == QueueNotFoundErrorCode)
            {
                return 0;
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

            Lazy<IStartStop> CreateProducer(RabbitMqSubscriptionSettings s)
            {
                // Lazy ensures RabbitMqPublisher will be created and started only once
                // https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
                return new Lazy<IStartStop>(() =>
                {
                    var publisher = new RabbitMqPublisher<TMessage>(_loggerFactory, s);

                    if (s.IsDurable && _publishingQueueRepository != null)
                        publisher.SetQueueRepository(_publishingQueueRepository);
                    else
                        publisher.DisableInMemoryQueuePersistence();

                    var result = publisher
                        .SetSerializer(serializer)
                        .SetWriteHeadersFunc(_correlationManager.BuildCorrelationHeadersIfExists);
                    result.Start();
                    return result;
                });
            }
        }
        
        public void Subscribe<TMessage>(RabbitMqSettings settings, 
            bool isDurable,
            Func<TMessage, Task> handler, 
            IMessageDeserializer<TMessage> deserializer)
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
                
                var rabbitMqSubscriber = new RabbitMqSubscriber<TMessage>(
                        _loggerFactory.CreateLogger<RabbitMqSubscriber<TMessage>>(),
                        subscriptionSettings)
                    .UseMiddleware(new ExceptionSwallowMiddleware<TMessage>(_loggerFactory.CreateLogger<ExceptionSwallowMiddleware<TMessage>>()))
                    .SetMessageDeserializer(deserializer)
                    .SetReadHeadersAction(_correlationManager.FetchCorrelationIfExists)
                    .Subscribe(handler);

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