using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Core;
using MarginTrading.Core.Monitoring;
using MarginTrading.Core.Settings;
using Newtonsoft.Json;

namespace MarginTrading.OrderHistoryBroker
{
    public class Application : TimerPeriod
    {
        private readonly IMarginTradingOrdersHistoryRepository _ordersHistoryRepository;
        private readonly IServiceMonitoringRepository _serviceMonitoringRepository;
        private readonly ILog _logger;
        private readonly MarginSettings _settings;
        private RabbitMqSubscriber<string> _connector;
        private const string ServiceName = "MarginTrading.OrderHistoryBroker";

        public Application(
            IMarginTradingOrdersHistoryRepository ordersHistoryRepository,
            IServiceMonitoringRepository serviceMonitoringRepository,
            ILog logger, MarginSettings settings) : base(ServiceName, 30000, logger)
        {
            _ordersHistoryRepository = ordersHistoryRepository;
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
                        ConnectionString = _settings.MtRabbitMqConnString,
                        QueueName = QueueHelper.BuildQueueName(_settings.RabbitMqQueues.OrderHistory.ExchangeName, _settings.Env),
                        ExchangeName = _settings.RabbitMqQueues.OrderHistory.ExchangeName,
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
            var order = JsonConvert.DeserializeObject<OrderFullContract>(json);
            var orderHistory = order.ToOrderHistoryDomain();
            await _ordersHistoryRepository.AddAsync(orderHistory);
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
