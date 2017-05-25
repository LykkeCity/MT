using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;
using MarginTrading.Core.Monitoring;
using MarginTrading.Core.Settings;
using Newtonsoft.Json;

namespace MarginTrading.AccountHistoryBroker
{
    public class Application : TimerPeriod
    {
        private readonly IMarginTradingAccountHistoryRepository _accountHistoryRepository;
        private readonly IServiceMonitoringRepository _serviceMonitoringRepository;
        private readonly ILog _logger;
        private readonly MarginSettings _settings;
        private RabbitMqSubscriber<string> _connector;
        private const string ServiceName = "MarginTrading.AccountHistoryBroker";

        public Application(
            IMarginTradingAccountHistoryRepository accountHistoryRepository,
            IServiceMonitoringRepository serviceMonitoringRepository,
            ILog logger, MarginSettings settings) : base(ServiceName, 30000, logger)
        {
            _accountHistoryRepository = accountHistoryRepository;
            _serviceMonitoringRepository = serviceMonitoringRepository;
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
                        ConnectionString = _settings.MarginTradingRabbitMqSettings.InternalConnectionString,
                        QueueName = _settings.RabbitMqQueues.AccountHistory.QueueName,
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

        public override async Task Execute()
        {
            var now = DateTime.UtcNow;

            var record = new MonitoringRecord
            {
                DateTime = now,
                ServiceName = ServiceName,
                Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion
            };

            await _serviceMonitoringRepository.UpdateOrCreate(record);
        }
    }
}
