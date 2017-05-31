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

namespace MarginTrading.RiskManagerBroker
{
	public class Application : TimerPeriod
	{
		private readonly IServiceMonitoringRepository _serviceMonitoringRepository;
		private readonly IRiskCalculationEngine _riskCalculationEngine;
		private readonly ILog _logger;
		private readonly MarginSettings _settings;
		private RabbitMqSubscriber<string> _connector;
		private const string ServiceName = "MarginTrading.RiskManagerBroker";

		public Application(
			IServiceMonitoringRepository serviceMonitoringRepository,
			IRiskCalculationEngine riskCalculationEngine,
			ILog logger, MarginSettings settings) : base(ServiceName, settings.RiskManagement.QuoteSamplingInterval, logger)
		{
			_serviceMonitoringRepository = serviceMonitoringRepository;
			_riskCalculationEngine = riskCalculationEngine;
			_logger = logger;
			_settings = settings;
		}

		public async Task RunAsync()
		{
			Start();

			await _logger.WriteInfoAsync(ServiceName, null, null, "Starting broker");

			try
			{
				await _riskCalculationEngine.InitializeAsync();

				_connector = new RabbitMqSubscriber<string>(new RabbitMqSubscriberSettings
				{
					ConnectionString = _settings.MtRabbitMqConnString,
					QueueName = _settings.RabbitMqQueues.ElementaryTransaction.QueueName,
					ExchangeName = _settings.RabbitMqQueues.ElementaryTransaction.ExchangeName,
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
			var transaction = JsonConvert.DeserializeObject<ElementaryTransaction>(json);

			await _riskCalculationEngine.ProcessTransactionAsync(transaction);
		}

		public override async Task Execute()
		{
			try
			{
				await _riskCalculationEngine.UpdateInternalStateAsync();

				var now = DateTime.UtcNow;

				var record = new MonitoringRecord
				{
					DateTime = now,
					ServiceName = ServiceName,
					Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion
				};

				await _serviceMonitoringRepository.UpdateOrCreate(record);
			}
			catch (Exception ex)
			{
				await _logger.WriteErrorAsync(ServiceName, "Application.Execute", null, ex);
			}
		}
	}
}