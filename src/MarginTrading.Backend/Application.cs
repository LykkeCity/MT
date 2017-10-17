using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Core;
using MarginTrading.Core.MarketMakerFeed;
using MarginTrading.Core.MatchingEngines;
using MarginTrading.Core.Settings;
using MarginTrading.Services.Infrastructure;
using MarginTrading.Services.MatchingEngines;
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
        private readonly IRabbitMqService _rabbitMqService;
        private readonly MatchingEngineRoutesManager _matchingEngineRoutesManager;
        private const string ServiceName = "MarginTrading.Backend";

        public Application(
            IRabbitMqNotifyService rabbitMqNotifyService,
            IConsole consoleWriter,
            IEnumerable<IFeedConsumer> consumers,
            ILog logger, MarginSettings marginSettings,
            IMaintenanceModeService maintenanceModeService,
            IRabbitMqService rabbitMqService,
            MatchingEngineRoutesManager matchingEngineRoutesManager)
        {
            _consumers = consumers.ToList();
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _consoleWriter = consoleWriter;
            _logger = logger;
            _marginSettings = marginSettings;
            _maintenanceModeService = maintenanceModeService;
            _rabbitMqService = rabbitMqService;
            _matchingEngineRoutesManager = matchingEngineRoutesManager;
        }

        public async Task StartApplicationAsync()
        {
            _consoleWriter.WriteLine($"Starting {ServiceName}");
            await _logger.WriteInfoAsync(ServiceName, null, null, "Starting broker");

            try
            {
                _rabbitMqService.Subscribe<MarketMakerOrderCommandsBatchMessage>(
                    _marginSettings.MarketMakerRabbitMqSettings, _marginSettings.Env, HandleNewOrdersMessage);

                _rabbitMqService.Subscribe<MatchingEngineRouteRisksCommand>(_marginSettings.RisksRabbitMqSettings,
                    _marginSettings.Env, _matchingEngineRoutesManager.HandleRiskManagerCommand);

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
            _rabbitMqNotifyService.Stop();
            _consoleWriter.WriteLine($"Closed {ServiceName}");
        }

        private Task HandleNewOrdersMessage(MarketMakerOrderCommandsBatchMessage feedData)
        {
            _consumers.ForEach(c => c.ConsumeFeed(feedData));
            return Task.CompletedTask;
        }
    }
}
