using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Core;
using MarginTrading.Core.Monitoring;
using MarginTrading.Core.Settings;
using Newtonsoft.Json;

namespace MarginTrading.OrderReportBroker
{
    public class Application : TimerPeriod
    {
        private readonly IMarginTradingOrderActionRepository _orderActionRepository;
        private readonly IServiceMonitoringRepository _serviceMonitoringRepository;
        private readonly IMarginTradingOrdersHistoryRepository _orderHistoryRepository;
        private readonly IOrderActionService _orderActionService;
        private readonly ILog _logger;
        private readonly MarginSettings _settings;
        private RabbitMqSubscriber<string> _connector;
        private const string ServiceName = "MarginTrading.OrderReportBroker";

        public Application(
            IMarginTradingOrderActionRepository orderActionRepository,
            IMarginTradingOrdersHistoryRepository orderHistoryRepository,
            IServiceMonitoringRepository serviceMonitoringRepository,
            IOrderActionService orderActionService,
            ILog logger, MarginSettings settings) : base(ServiceName, 30000, logger)
        {
            _orderActionRepository = orderActionRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _serviceMonitoringRepository = serviceMonitoringRepository;
            _orderActionService = orderActionService;
            _logger = logger;
            _settings = settings;
        }

        public async Task RunAsync()
        {
            Start();

            await _logger.WriteInfoAsync(ServiceName, null, null, "Starting broker");

            try
            {
                if (!_orderActionRepository.Any())
                {
                    await _logger.WriteInfoAsync(ServiceName, null, null, "Restoring order actions from order hisorical data");

                    await _orderActionService.CreateOrderActionsForOrderHistory(
                            _orderHistoryRepository.GetHistoryAsync,
                            _orderActionRepository.AddAsync
                        );
                }

                _connector = new RabbitMqSubscriber<string>(new RabbitMqSubscriberSettings
                {
                    ConnectionString = _settings.MarginTradingRabbitMqSettings.InternalConnectionString,
                    QueueName = _settings.RabbitMqQueues.OrderReport.QueueName,
                    ExchangeName = _settings.MarginTradingRabbitMqSettings.ExchangeName,
                    IsDurable = true
                })
                    .SetMessageDeserializer(new DefaultStringDeserializer())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(_settings.RabbitMqQueues.OrderReport.RoutingKeyName))
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
            var order = JsonConvert.DeserializeObject<OrderAction>(json);

            await _orderActionRepository.AddAsync(order);
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