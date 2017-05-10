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

namespace MarginTrading.TransactionBroker
{
    public class Application : TimerPeriod
    {
        private readonly IMarginTradingTransactionRepository _transactionRepository;
        private readonly IServiceMonitoringRepository _serviceMonitoringRepository;
        private readonly IMarginTradingOrdersHistoryRepository _orderHistoryRepository;
        private readonly ITransactionService _transactionService;
        private readonly IElementaryTransactionService _elementaryTransactionService;
        private readonly ILog _logger;
        private readonly MarginSettings _settings;
        private RabbitMqSubscriber<string> _connector;
        private const string ServiceName = "MarginTrading.TransactionBroker";

        public Application(
            IMarginTradingTransactionRepository transactionRepository,
            IMarginTradingOrdersHistoryRepository orderHistoryRepository,
            IServiceMonitoringRepository serviceMonitoringRepository,
            ITransactionService transactionService,
            IElementaryTransactionService elementaryTransactionService,
            ILog logger, MarginSettings settings) : base(ServiceName, 30000, logger)
        {
            _transactionRepository = transactionRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _serviceMonitoringRepository = serviceMonitoringRepository;
            _elementaryTransactionService = elementaryTransactionService;
            _transactionService = transactionService;
            _logger = logger;
            _settings = settings;
        }

        public async Task RunAsync()
        {
            Start();

            await _logger.WriteInfoAsync(ServiceName, null, null, "Starting broker");

            try
            {
                if (!_transactionRepository.Any())
                {
                    await _logger.WriteInfoAsync(ServiceName, null, null, "Restoring transactions from order hisorical data");

                    await _transactionService.CreateTransactionsForOrderHistory(
                            _orderHistoryRepository.GetHistoryAsync,
                            _transactionRepository.AddAsync
                        );
                }

                if (!_elementaryTransactionService.Any())
                {
                    await _logger.WriteInfoAsync(ServiceName, null, null, "Restoring elementary transactions from historical data");

                    await _elementaryTransactionService.CreateElementaryTransactionsFromTransactionReport();
                }

                _connector = new RabbitMqSubscriber<string>(new RabbitMqSubscriberSettings
                {
                    ConnectionString = _settings.MarginTradingRabbitMqSettings.InternalConnectionString,
                    QueueName = _settings.RabbitMqQueues.Transaction.QueueName,
                    ExchangeName = _settings.MarginTradingRabbitMqSettings.ExchangeName,
                    IsDurable = true
                })
                    .SetMessageDeserializer(new DefaultStringDeserializer())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(_settings.RabbitMqQueues.Transaction.RoutingKeyName))
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
            var transaction = JsonConvert.DeserializeObject<Transaction>(json);

            await _transactionRepository.AddAsync(transaction);

            await _elementaryTransactionService.CreateElementaryTransactionsAsync(transaction);
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
