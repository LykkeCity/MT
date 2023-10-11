// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;
using Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling;
using Lykke.Snow.Common.Correlation.RabbitMq;
using MarginTrading.Common.Services;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace MarginTrading.Common.RabbitMq
{
    public sealed class RabbitMqService : IRabbitMqService, IDisposable
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
        
        private readonly ConcurrentDictionary<string, IAutorecoveringConnection> Connections =
            new ConcurrentDictionary<string, IAutorecoveringConnection>();

        private const short QueueNotFoundErrorCode = 404;
        private const int PrefetchCount = 200;

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
            using var connection = CreateConnection(connectionString);
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
            
            foreach (var connection in Connections.Values)
            {
                DetachConnectionEventHandlers(connection);
                connection.Dispose();
            }
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

        public Lykke.RabbitMqBroker.Publisher.IMessageProducer<TMessage> GetProducer<TMessage>(RabbitMqSettings settings,
            IRabbitMqSerializer<TMessage> serializer)
        {
            // on-the fly connection strings switch is not supported currently for rabbitMq
            var subscriptionSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.ConnectionString,
                ExchangeName = settings.ExchangeName,
                IsDurable = settings.IsDurable,
            };

            return (Lykke.RabbitMqBroker.Publisher.IMessageProducer<TMessage>) _producers.GetOrAdd(subscriptionSettings, CreateProducer).Value;

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
                        subscriptionSettings,
                        GetConnection(settings.ConnectionString, false))
                    .UseMiddleware(new ExceptionSwallowMiddleware<TMessage>(_loggerFactory.CreateLogger<ExceptionSwallowMiddleware<TMessage>>()))
                    .SetMessageDeserializer(deserializer)
                    .SetReadHeadersAction(_correlationManager.FetchCorrelationIfExists)
                    .SetPrefetchCount(PrefetchCount)
                    .Subscribe(handler);

                if (!_subscribers.TryAdd((subscriptionSettings, consumerNumber), rabbitMqSubscriber))
                {
                    throw new InvalidOperationException(
                        $"A subscriber number {consumerNumber} for queue {subscriptionSettings.QueueName} was already initialized");
                }

                rabbitMqSubscriber.Start();
            }
        }

        #region Connection establishment

        private IAutorecoveringConnection GetConnection(string connectionString, bool reuse = true)
        {
            var exists = Connections.TryGetValue(connectionString, out var connection);
            if (exists && reuse)
                return connection;

            connection = CreateConnection(connectionString);

            var key = exists ? Guid.NewGuid().ToString("N") : connectionString;
            if (!Connections.TryAdd(key, connection))
            {
                key = Guid.NewGuid().ToString("N");
                Connections.TryAdd(key, connection);
            }
            
            AttachConnectionEventHandlers(connection);
            
            return connection;
        }

        private static IAutorecoveringConnection CreateConnection(string connectionString)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString, UriKind.Absolute),
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(60),
                ContinuationTimeout = TimeSpan.FromSeconds(30),
                ClientProvidedName = typeof(RabbitMqService).FullName
            };
            
            return factory.CreateConnection() as IAutorecoveringConnection;
        }
        
        private static void AttachConnectionEventHandlers(IAutorecoveringConnection connection)
        {
            connection.RecoverySucceeded += OnRecoverySucceeded;
            connection.ConnectionBlocked += OnConnectionBlocked;
            connection.ConnectionShutdown += OnConnectionShutdown;
            connection.ConnectionUnblocked += OnConnectionUnblocked;
            connection.CallbackException += OnCallbackException;
            connection.ConnectionRecoveryError += OnConnectionRecoveryError;
        }

        private static void DetachConnectionEventHandlers(IAutorecoveringConnection connection)
        {
            connection.RecoverySucceeded -= OnRecoverySucceeded;
            connection.ConnectionBlocked -= OnConnectionBlocked;
            connection.ConnectionShutdown -= OnConnectionShutdown;
            connection.ConnectionUnblocked -= OnConnectionUnblocked;
            connection.CallbackException -= OnCallbackException;
            connection.ConnectionRecoveryError -= OnConnectionRecoveryError;
        }
        
        private static void OnRecoverySucceeded(object sender, EventArgs e)
        {
            LogLocator.CommonLog.WriteInfo(nameof(RabbitMqService), null, "RabbitMq connection recovered");
        } 
        
        private static void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            LogLocator.CommonLog.WriteWarning(nameof(RabbitMqService), new { Reason = e.Reason }.ToJson(),
                "RabbitMq connection blocked");
        }
        
        private static void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            LogLocator.CommonLog.WriteWarning(nameof(RabbitMqService),
                new { Initiator = e.Initiator.ToString(), e.ReplyCode, e.ReplyText, e.MethodId }.ToJson(),
                "RabbitMq connection shutdown");

            if (e.Cause != null)
            {
                LogLocator.CommonLog.WriteWarning(nameof(RabbitMqService),
                    new { CauseObjectType = e.Cause.GetType().FullName }, "Object causing the shutdown");
            }
        }
        
        private static void OnConnectionUnblocked(object sender, EventArgs e)
        {
            LogLocator.CommonLog.WriteInfo(nameof(RabbitMqService), null, "RabbitMq connection unblocked");
        }
        
        private static void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            LogLocator.CommonLog.WriteError(nameof(RabbitMqService), null, e.Exception);
        }
        
        private static void OnConnectionRecoveryError(object sender, ConnectionRecoveryErrorEventArgs e)
        {
            LogLocator.CommonLog.WriteError(nameof(RabbitMqService), null, e.Exception);
        }
        
        # endregion

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