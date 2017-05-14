using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Core;
using MarginTrading.Core.Monitoring;
using MarginTrading.Core.Settings;

namespace MarginTrading.IVaRBroker
{
    public class Application : TimerPeriod
    {
        private readonly IMarginTradingIndividualValuesAtRiskRepository _iVaRRepository;
        private readonly IServiceMonitoringRepository _serviceMonitoringRepository;
		private readonly ILog _logger;
        private readonly MarginSettings _settings;
        private RabbitMqSubscriber<string> _connector;
        private const string ServiceName = "MarginTrading.IVaRBroker";

		public Application(
			IMarginTradingIndividualValuesAtRiskRepository iVaRRepository,
			IServiceMonitoringRepository serviceMonitoringRepository,
			ILog logger, MarginSettings settings) : base(ServiceName, 30000, logger)
		{
			_iVaRRepository = iVaRRepository;
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
					QueueName = _settings.RabbitMqQueues.IndividualValuesAtRisk.QueueName,
					ExchangeName = _settings.MarginTradingRabbitMqSettings.ExchangeName,
					IsDurable = true
				})
					.SetMessageDeserializer(new DefaultStringDeserializer())
					.SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(_settings.RabbitMqQueues.IndividualValuesAtRisk.RoutingKeyName))
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
			string assetId = payload.Split(';')[1];
			double value = double.Parse(payload.Split(';')[2]);

			await _iVaRRepository.InsertOrUpdateAsync(counterPartyId, assetId, value);
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
