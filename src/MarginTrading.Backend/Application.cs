using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Core;
using MarginTrading.Core.MarketMakerFeed;
using MarginTrading.Core.Monitoring;
using MarginTrading.Core.Settings;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
#pragma warning disable 1591

namespace MarginTrading.Backend
{
    public sealed class Application : TimerPeriod
    {
        private readonly List<IFeedConsumer> _consumers;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IConsole _consoleWriter;
        private readonly IServiceMonitoringRepository _serviceMonitoringRepository;
        private readonly ILog _logger;
        private readonly MarginSettings _marginSettings;
        private RabbitMqSubscriber<MarketMakerOrderBook> _connector;
        private const string ServiceName = "MarginTrading.Backend";

        public Application(
            IRabbitMqNotifyService rabbitMqNotifyService,
            IConsole consoleWriter,
            IEnumerable<IFeedConsumer> consumers,
            IServiceMonitoringRepository serviceMonitoringRepository,
            ILog logger, MarginSettings marginSettings) : base(ServiceName, 30000, logger)
        {
            _consumers = consumers.ToList();
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _consoleWriter = consoleWriter;
            _serviceMonitoringRepository = serviceMonitoringRepository;
            _logger = logger;
            _marginSettings = marginSettings;
        }

        public async Task StartApplicatonAsync()
        {
            _consoleWriter.WriteLine($"Staring {ServiceName}");
            await _logger.WriteInfoAsync(ServiceName, null, null, "Starting broker");

            try
            {
                _connector = new RabbitMqSubscriber<MarketMakerOrderBook>(new RabbitMqSubscriberSettings
                    {
                        ConnectionString = _marginSettings.SpotRabbitMqSettings.ConnectionString,
                        QueueName = QueueHelper.BuildQueueName(_marginSettings.SpotRabbitMqSettings.ExchangeName, _marginSettings.IsLive ? "Live" : "Demo"),
                        ExchangeName = _marginSettings.SpotRabbitMqSettings.ExchangeName,
                        IsDurable = _marginSettings.SpotRabbitMqSettings.IsDurable
                    })
                    .SetMessageDeserializer(new BackEndDeserializer<MarketMakerOrderBook>())
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
            _consoleWriter.WriteLine($"Closing {ServiceName}");
            _logger.WriteInfoAsync(ServiceName, null, null, "Closing broker").Wait();
            _connector.Stop();
            _consumers.ForEach(c => c.ShutdownApplication());
            _rabbitMqNotifyService.Stop(); 
            Stop();
        }

        private Task HandleMessage(MarketMakerOrderBook orderBook)
        {
            if (orderBook.Prices.Any())
            {
                IAssetPairRate rate = AssetPairRate.Create(orderBook);
                _consumers.ForEach(c => c.ConsumeFeed(new[] { rate }));
            }
            
            return Task.FromResult(0);
        }

        public override async Task Execute()
        {
            var now = DateTime.UtcNow;

            var record = new MonitoringRecord
            {
                DateTime = now,
                ServiceName = ServiceName,
                Version = PlatformServices.Default.Application.ApplicationVersion
            };

            await _serviceMonitoringRepository.UpdateOrCreate(record);
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
