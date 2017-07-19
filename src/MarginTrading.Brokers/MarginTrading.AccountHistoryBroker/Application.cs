using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using Newtonsoft.Json;

namespace MarginTrading.AccountHistoryBroker
{
    public class Application
    {
        private readonly IMarginTradingAccountHistoryRepository _accountHistoryRepository;
        private readonly ILog _logger;
        private readonly MarginSettings _settings;
        private RabbitMqSubscriber<string> _connector;
        private const string ServiceName = "MarginTrading.AccountHistoryBroker";

        public Application(
            IMarginTradingAccountHistoryRepository accountHistoryRepository,
            ILog logger, MarginSettings settings)
        {
            _accountHistoryRepository = accountHistoryRepository;
            _logger = logger;
            _settings = settings;
        }

        public async Task RunAsync()
        {
            await _logger.WriteInfoAsync(ServiceName, null, null, "Starting broker");
            try
            {
                _connector = new RabbitMqSubscriber<string>(new RabbitMqSubscriberSettings
                    {
                        ConnectionString = _settings.MtRabbitMqConnString,
                        QueueName = QueueHelper.BuildQueueName(_settings.RabbitMqQueues.AccountHistory.ExchangeName, _settings.Env),
                        ExchangeName = _settings.RabbitMqQueues.AccountHistory.ExchangeName,
                        IsDurable = true
                    })
                    .SetMessageDeserializer(new DefaultStringDeserializer())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .Subscribe(HandleMessage)
                    .SetLogger(_logger)
                    .Start();
            }
            catch (Exception ex)
            {
                await _logger.WriteErrorAsync(ServiceName, "Application.RunAsync", null, ex);
            }
        }

        public void StopApplication()
        {
            Console.WriteLine($"Closing {ServiceName}...");
             _logger.WriteInfoAsync(ServiceName, null, null, "Stopping broker").Wait();
            _connector.Stop();
        }

        private async Task HandleMessage(string json)
        {
            var accountHistoryContract = JsonConvert.DeserializeObject<AccountHistoryBackendContract>(json);
            var accountHistory = accountHistoryContract.ToAccountHistoryContract();
            await _accountHistoryRepository.AddAsync(accountHistory);
        }
    }
}
