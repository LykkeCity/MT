using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Core;
using MarginTrading.Core.MarketMakerFeed;
using MarginTrading.Core.Settings;
using MarginTrading.Services.Infrastructure;
using Newtonsoft.Json;

#pragma warning disable 1591

namespace MarginTrading.Backend
{
    public sealed class Application
    {
        private readonly List<IFeedConsumer> _consumers;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IConsole _consoleWriter;
        private readonly ILog _logger;
        private readonly MarginSettings _marginSettings;
        private readonly IMaintenanceModeService _maintenanceModeService;
        private RabbitMqSubscriber<MarketMakerOrderCommandsBatchMessage> _connector;
        private const string ServiceName = "MarginTrading.Backend";

        public Application(
            IRabbitMqNotifyService rabbitMqNotifyService,
            IConsole consoleWriter,
            IEnumerable<IFeedConsumer> consumers,
            ILog logger, MarginSettings marginSettings,
            IMaintenanceModeService maintenanceModeService)
        {
            _consumers = consumers.ToList();
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _consoleWriter = consoleWriter;
            _logger = logger;
            _marginSettings = marginSettings;
            _maintenanceModeService = maintenanceModeService;
        }

        public async Task StartApplicationAsync()
        {
            _consoleWriter.WriteLine($"Staring {ServiceName}");
            await _logger.WriteInfoAsync(ServiceName, null, null, "Starting broker");

            try
            {
                var settings = new RabbitMqSubscriptionSettings
                {
                    ConnectionString = _marginSettings.MarketMakerRabbitMqSettings.ConnectionString,
                    QueueName = QueueHelper.BuildQueueName(_marginSettings.MarketMakerRabbitMqSettings.ExchangeName, _marginSettings.IsLive ? "Live" : "Demo"),
                    ExchangeName = _marginSettings.MarketMakerRabbitMqSettings.ExchangeName,
                    IsDurable = _marginSettings.MarketMakerRabbitMqSettings.IsDurable
                };
                _connector = new RabbitMqSubscriber<MarketMakerOrderCommandsBatchMessage>(settings, new DefaultErrorHandlingStrategy(_logger, settings))
                    .SetMessageDeserializer(new BackEndDeserializer<MarketMakerOrderCommandsBatchMessage>())
                    .Subscribe(HandleMessage)
                    .SetLogger(_logger)
                    .SetConsole(_consoleWriter)
                    .Start();
            }
            catch (Exception ex)
            {
                _consoleWriter.WriteLine($"{ServiceName} error: {ex.Message}");
                await _logger.WriteErrorAsync(ServiceName, "Application.RunAsync", null, ex);
            }
        }

        public void StopApplication()
        {
            _maintenanceModeService.SetMode(true);
            _consoleWriter.WriteLine($"Maintenance mode enabled for {ServiceName}");
            _consoleWriter.WriteLine($"Closing {ServiceName}");
            _logger.WriteInfoAsync(ServiceName, null, null, "Closing broker").Wait();
            _connector.Stop();
            _rabbitMqNotifyService.Stop();
            _consoleWriter.WriteLine($"Closed {ServiceName}");
        }

        private Task HandleMessage(MarketMakerOrderCommandsBatchMessage feedData)
        {
            _consumers.ForEach(c => c.ConsumeFeed(feedData));
            return Task.CompletedTask;
        }
    }

    public class BackEndDeserializer<T> : IMessageDeserializer<T>
    {
        public T Deserialize(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
