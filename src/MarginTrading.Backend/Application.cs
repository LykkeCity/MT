using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using FluentScheduler;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MarketMakerFeed;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Scheduling;
using MarginTrading.Backend.Services.Helpers;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Backend.Services.Stp;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services;
using MarginTrading.OrderbookAggregator.Contracts.Messages;
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
        private readonly IConvertService _convertService;
        private readonly ExternalOrderBooksList _externalOrderBooksList;
        private const string ServiceName = "MarginTrading.Backend";

        public Application(
            IRabbitMqNotifyService rabbitMqNotifyService,
            IConsole consoleWriter,
            MarketMakerService marketMakerService,
            ILog logger, 
            MarginSettings marginSettings,
            IMaintenanceModeService maintenanceModeService,
            IRabbitMqService rabbitMqService,
            MatchingEngineRoutesManager matchingEngineRoutesManager,
            IConvertService convertService,
            ExternalOrderBooksList externalOrderBooksList)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _consoleWriter = consoleWriter;
            _marketMakerService = marketMakerService;
            _logger = logger;
            _marginSettings = marginSettings;
            _maintenanceModeService = maintenanceModeService;
            _rabbitMqService = rabbitMqService;
            _matchingEngineRoutesManager = matchingEngineRoutesManager;
            _convertService = convertService;
            _externalOrderBooksList = externalOrderBooksList;
        }

        public async Task StartApplicationAsync()
        {
            _consoleWriter.WriteLine($"Starting {ServiceName}");
            await _logger.WriteInfoAsync(ServiceName, null, null, "Starting broker");

            try
            {
                _rabbitMqService.Subscribe(
                    _marginSettings.MarketMakerRabbitMqSettings, false, HandleNewOrdersMessage,
                    _rabbitMqService.GetJsonDeserializer<MarketMakerOrderCommandsBatchMessage>());

                if (_marginSettings.RisksRabbitMqSettings != null)
                {
                    _rabbitMqService.Subscribe(_marginSettings.RisksRabbitMqSettings,
                        true, _matchingEngineRoutesManager.HandleRiskManagerCommand,
                        _rabbitMqService.GetJsonDeserializer<MatchingEngineRouteRisksCommand>());
                }
                else if (_marginSettings.IsLive)
                {
                    _logger.WriteWarning(ServiceName, nameof(StartApplicationAsync),
                        "RisksRabbitMqSettings is not configured");
                }
                
                // Demo server works only in MM mode
                if (_marginSettings.IsLive)
                {
                    _rabbitMqService.Subscribe(_marginSettings.StpAggregatorRabbitMqSettings
                            .RequiredNotNull(nameof(_marginSettings.StpAggregatorRabbitMqSettings)), false, 
                        HandleStpOrderbook,
                        _rabbitMqService.GetMsgPackDeserializer<ExternalExchangeOrderbookMessage>());
                }

                var settingsCalcTime = OvernightSwapHelpers.GetOvernightSwapCalcTime(_marginSettings.OvernightSwapCalculationTime);
                var registry = new Registry();
                registry.Schedule<OvernightSwapJob>().ToRunEvery(0).Days().At(settingsCalcTime.Hour, settingsCalcTime.Min);
                JobManager.Initialize(registry);
                JobManager.JobException += info => _logger.WriteError(ServiceName, nameof(JobManager), info.Exception);
            }
            catch (Exception ex)
            {
                _consoleWriter.WriteLine($"{ServiceName} error: {ex.Message}");
                await _logger.WriteErrorAsync(ServiceName, "Application.RunAsync", null, ex);
            }
        }

        private Task HandleStpOrderbook(ExternalExchangeOrderbookMessage message)
        {
            var orderbook = _convertService.Convert<ExternalExchangeOrderbookMessage, ExternalOrderBook>(message);
            _externalOrderBooksList.SetOrderbook(orderbook);
            return Task.CompletedTask;
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