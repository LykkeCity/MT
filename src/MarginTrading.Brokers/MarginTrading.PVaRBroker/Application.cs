using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Core;
using MarginTrading.Core.Monitoring;
using MarginTrading.Core.Settings;

namespace MarginTrading.PVaRBroker
{
    public class Application : TimerPeriod
    {
        private readonly IMarginTradingAggregateValuesAtRiskRepository _pVaRRepository;
        private readonly IServiceMonitoringRepository _serviceMonitoringRepository;
		private readonly ILog _logger;
        private readonly MarginSettings _settings;
        private RabbitMqSubscriber<string> _connector;
        private const string ServiceName = "MarginTrading.PVaRBroker";

		public Application(
			IMarginTradingAggregateValuesAtRiskRepository pVaRRepository,
			IServiceMonitoringRepository serviceMonitoringRepository,
			ILog logger, MarginSettings settings) : base(ServiceName, 30000, logger)
		{
			_pVaRRepository = pVaRRepository;
			_serviceMonitoringRepository = serviceMonitoringRepository;
			_logger = logger;
			_settings = settings;
		}

		public async Task RunAsync()
		{
			Start();

			await _logger.WriteInfoAsync(ServiceName, null, null, "Starting broker");

			try
			{
				_connector = new RabbitMqSubscriber<string>(new RabbitMqSubscriberSettings
				{
					ConnectionString = _settings.MarginTradingRabbitMqSettings.InternalConnectionString,
					QueueName = _settings.RabbitMqQueues.AggregateValuesAtRisk.QueueName,
					ExchangeName = _settings.MarginTradingRabbitMqSettings.ExchangeName,
					IsDurable = true
				})
					.SetMessageDeserializer(new DefaultStringDeserializer())
					.SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(_settings.RabbitMqQueues.AggregateValuesAtRisk.RoutingKeyName))
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

		private async Task HandleMessage(string payload)
		{
			string counterPartyId = payload.Split(';')[0];
			double value = double.Parse(payload.Split(';')[1]);

			await _pVaRRepository.InsertOrUpdateAsync(counterPartyId, value);
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
