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

namespace MarginTrading.ElementaryTransactionBroker
{
	public class Application : TimerPeriod
	{
		private readonly IServiceMonitoringRepository _serviceMonitoringRepository;
		private readonly ILog _logger;
		private readonly MarginSettings _settings;
		private RabbitMqSubscriber<string> _connector;
		private readonly IElementaryTransactionsRepository _elementaryTransactionsRepository;
		private const string ServiceName = "MarginTrading.ElementaryTransactionBroker";

		public Application(
			IServiceMonitoringRepository serviceMonitoringRepository,
			IElementaryTransactionsRepository elementaryTransactionsRepository,
			ILog logger, MarginSettings settings) : base(ServiceName, settings.RiskManagement.QuoteSamplingInterval, logger)
		{
			_serviceMonitoringRepository = serviceMonitoringRepository;
			_elementaryTransactionsRepository = elementaryTransactionsRepository;
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
					QueueName = _settings.RabbitMqQueues.ElementaryTransaction.QueueName,
					ExchangeName = _settings.MarginTradingRabbitMqSettings.ExchangeName,
					IsDurable = true
				})
					.SetMessageDeserializer(new DefaultStringDeserializer())
					.SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(_settings.RabbitMqQueues.ElementaryTransaction.RoutingKeyName))
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
			var elementaryTransaction = JsonConvert.DeserializeObject<ElementaryTransaction>(json);

			await _elementaryTransactionsRepository.AddAsync(elementaryTransaction);
		}

		public override async Task Execute()
		{
			try
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
			catch (Exception ex)
			{
				await _logger.WriteErrorAsync(ServiceName, "Application.Execute", null, ex);
			}
		}
	}
}