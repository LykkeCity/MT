using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.BrokerBase.Settings;

namespace MarginTrading.BrokerBase
{
    public abstract class BrokerApplicationBase<TMessage> : IBrokerApplication
    {
        private readonly ILog _logger;
        protected readonly CurrentApplicationInfo _applicationInfo;
        private RabbitMqSubscriber<TMessage> _connector;

        protected abstract RabbitMqSubscriptionSettings GetRabbitMqSubscriptionSettings();

        protected abstract Task HandleMessage(TMessage message);

        protected BrokerApplicationBase(ILog logger, CurrentApplicationInfo applicationInfo)
        {
            _logger = logger;
            _applicationInfo = applicationInfo;
        }

        public virtual void Run()
        {
            _logger.WriteInfoAsync(_applicationInfo.ApplicationName, null, null, "Starting broker");
            try
            {
                var settings = GetRabbitMqSubscriptionSettings();
                _connector =
                    new RabbitMqSubscriber<TMessage>(settings,
                            new ResilientErrorHandlingStrategy(_logger, settings, TimeSpan.FromSeconds(1)))
                        .SetMessageDeserializer(new JsonMessageDeserializer<TMessage>())
                        .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                        .Subscribe(HandleMessage)
                        .SetLogger(_logger)
                        .Start();
                _logger.WriteInfoAsync(_applicationInfo.ApplicationName, null, null,
                    "Broker listening queue " + settings.QueueName);
            }
            catch (Exception ex)
            {
                _logger.WriteErrorAsync(_applicationInfo.ApplicationName, "Application.RunAsync", null, ex).GetAwaiter()
                    .GetResult();
            }
        }

        public void StopApplication()
        {
            Console.WriteLine($"Closing {_applicationInfo.ApplicationName}...");
            _logger.WriteInfoAsync(_applicationInfo.ApplicationName, null, null, "Stopping broker").Wait();
            _connector.Stop();
        }
    }
}