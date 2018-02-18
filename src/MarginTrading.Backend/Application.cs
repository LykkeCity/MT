using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MarketMakerFeed;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services;
using Newtonsoft.Json;

#pragma warning disable 1591

namespace MarginTrading.Backend
{
    public sealed class Application
    {
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IConsole _consoleWriter;
        private readonly MarketMakerService _marketMakerService;
        private readonly ILog _logger;
        private readonly MarginSettings _marginSettings;
        private readonly IMaintenanceModeService _maintenanceModeService;
        private readonly IRabbitMqService _rabbitMqService;
        private readonly MatchingEngineRoutesManager _matchingEngineRoutesManager;
        private const string ServiceName = "MarginTrading.Backend";

        public Application(
            IRabbitMqNotifyService rabbitMqNotifyService,
            IConsole consoleWriter,
            MarketMakerService marketMakerService,
            ILog logger, MarginSettings marginSettings,
            IMaintenanceModeService maintenanceModeService,
            IRabbitMqService rabbitMqService,
            MatchingEngineRoutesManager matchingEngineRoutesManager)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _consoleWriter = consoleWriter;
            _marketMakerService = marketMakerService;
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

                if (_marginSettings.RisksRabbitMqSettings != null)
                {
                    _rabbitMqService.Subscribe<MatchingEngineRouteRisksCommand>(_marginSettings.RisksRabbitMqSettings,
                        _marginSettings.Env, _matchingEngineRoutesManager.HandleRiskManagerCommand);
                }
                else if (_marginSettings.IsLive)
                {
                    _logger.WriteWarning(ServiceName, nameof(StartApplicationAsync),
                        "RisksRabbitMqSettings is not configured");
                }
                
                // Demo server works only in MM mode

                if (_marginSettings.IsLive)
                {
                    //TODO: subscribe to STP orderbooks
                }

            }
            catch (Exception ex)
            {
                _consoleWriter.WriteLine($"{ServiceName} error: {ex.Message}");
                await _logger.WriteErrorAsync(ServiceName, nameof(StartApplicationAsync), null, ex);
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
            _marketMakerService.ProcessOrderCommands(feedData);
            return Task.CompletedTask;
        }
    }
}
