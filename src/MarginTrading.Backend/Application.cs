using System;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MarketMakerFeed;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Backend.Services.Stp;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services;
using MarginTrading.OrderbookAggregator.Contracts.Messages;
using MarginTrading.SettingsService.Contracts.AssetPair;
using MarginTrading.SettingsService.Contracts.Enums;
using MarginTrading.SettingsService.Contracts.Messages;

#pragma warning disable 1591

namespace MarginTrading.Backend
{
    public sealed class Application
    {
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IConsole _consoleWriter;
        private readonly MarketMakerService _marketMakerService;
        private readonly ILog _logger;
        private readonly MarginTradingSettings _marginSettings;
        private readonly IMaintenanceModeService _maintenanceModeService;
        private readonly IRabbitMqService _rabbitMqService;
        private readonly IMatchingEngineRoutesManager _matchingEngineRoutesManager;
        private readonly IMigrationService _migrationService;
        private readonly IConvertService _convertService;
        private readonly IFxRateCacheService _fxRateCacheService; 
        private readonly IExternalOrderbookService _externalOrderbookService;
        private readonly IAssetsManager _assetsManager;
        private readonly IAssetPairsManager _assetPairsManager;
        private readonly ITradingInstrumentsManager _tradingInstrumentsManager;
        private readonly ITradingConditionsManager _tradingConditionsManager;
        private const string ServiceName = "MarginTrading.Backend";

        public Application(
            IRabbitMqNotifyService rabbitMqNotifyService,
            IConsole consoleWriter,
            MarketMakerService marketMakerService,
            ILog logger, 
            MarginTradingSettings marginSettings,
            IMaintenanceModeService maintenanceModeService,
            IRabbitMqService rabbitMqService,
            MatchingEngineRoutesManager matchingEngineRoutesManager,
            IMigrationService migrationService,
            IConvertService convertService,
            IFxRateCacheService fxRateCacheService, 
            IExternalOrderbookService externalOrderbookService,
            IAssetsManager assetsManager,
            IAssetPairsManager assetPairsManager,
            ITradingInstrumentsManager tradingInstrumentsManager,
            ITradingConditionsManager tradingConditionsManager)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _consoleWriter = consoleWriter;
            _marketMakerService = marketMakerService;
            _logger = logger;
            _marginSettings = marginSettings;
            _maintenanceModeService = maintenanceModeService;
            _rabbitMqService = rabbitMqService;
            _matchingEngineRoutesManager = matchingEngineRoutesManager;
            _migrationService = migrationService;
            _convertService = convertService;
            _fxRateCacheService = fxRateCacheService; 
            _externalOrderbookService = externalOrderbookService;
            _assetsManager = assetsManager;
            _assetPairsManager = assetPairsManager;
            _tradingInstrumentsManager = tradingInstrumentsManager;
            _tradingConditionsManager = tradingConditionsManager;
        }

        public async Task StartApplicationAsync()
        {
            _consoleWriter.WriteLine($"Starting {ServiceName}");
            await _logger.WriteInfoAsync(ServiceName, null, null, "Starting...");

            if (_marginSettings.MarketMakerRabbitMqSettings == null &&
                _marginSettings.StpAggregatorRabbitMqSettings == null)
            {
                throw new Exception("Both MM and STP connections are not configured. Can not start service.");
            }

            try
            {
                await _migrationService.InvokeAll();
                
                if (_marginSettings.MarketMakerRabbitMqSettings != null)
                {
                    _rabbitMqService.Subscribe(
                        _marginSettings.MarketMakerRabbitMqSettings, false, HandleNewOrdersMessage,
                        _rabbitMqService.GetJsonDeserializer<MarketMakerOrderCommandsBatchMessage>());
                }
                else
                {
                    _logger.WriteInfo(ServiceName, nameof(StartApplicationAsync),
                        "MarketMakerRabbitMqSettings is not configured");
                }

                if (_marginSettings.FxRateRabbitMqSettings != null)
                {
                    _rabbitMqService.Subscribe(_marginSettings.FxRateRabbitMqSettings, false,
                        _fxRateCacheService.SetQuote, _rabbitMqService.GetMsgPackDeserializer<ExternalExchangeOrderbookMessage>());
                }
                
                if (_marginSettings.StpAggregatorRabbitMqSettings != null)
                {
                    _rabbitMqService.Subscribe(_marginSettings.StpAggregatorRabbitMqSettings,
                        false, HandleStpOrderbook,
                        _rabbitMqService.GetMsgPackDeserializer<ExternalExchangeOrderbookMessage>());
                }
                else
                {
                    _logger.WriteInfo(ServiceName, nameof(StartApplicationAsync),
                        "StpAggregatorRabbitMqSettings is not configured");
                }

                if (_marginSettings.RisksRabbitMqSettings != null)
                {
                    _rabbitMqService.Subscribe(_marginSettings.RisksRabbitMqSettings,
                        true, _matchingEngineRoutesManager.HandleRiskManagerCommand,
                        _rabbitMqService.GetJsonDeserializer<MatchingEngineRouteRisksCommand>());
                }
                else
                {
                    _logger.WriteInfo(ServiceName, nameof(StartApplicationAsync),
                        "RisksRabbitMqSettings is not configured");
                }

                var settingsChanged = new RabbitMqSettings
                {
                    ConnectionString = _marginSettings.MtRabbitMqConnString,
                    ExchangeName = _marginSettings.RabbitMqQueues.SettingsChanged.ExchangeName
                };

                _rabbitMqService.Subscribe(settingsChanged,
                    true, HandleChangeSettingsMessage,
                    _rabbitMqService.GetJsonDeserializer<SettingsChangedEvent>());
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
            _externalOrderbookService.SetOrderbook(orderbook);
            return Task.CompletedTask;
        }

        public void StopApplication()
        {
            _maintenanceModeService.SetMode(true);
            _consoleWriter.WriteLine($"Maintenance mode enabled for {ServiceName}");
            _consoleWriter.WriteLine($"Closing {ServiceName}");
            _logger.WriteInfoAsync(ServiceName, null, null, "Closing...").Wait();
            _rabbitMqNotifyService.Stop();
            _consoleWriter.WriteLine($"Closed {ServiceName}");
        }

        private Task HandleNewOrdersMessage(MarketMakerOrderCommandsBatchMessage feedData)
        {
            _marketMakerService.ProcessOrderCommands(feedData);
            return Task.CompletedTask;
        }

        private Task HandleChangeSettingsMessage(SettingsChangedEvent message)
        {
            switch (message.SettingsType)
            {
                case SettingsTypeContract.Asset:
                    _assetsManager.UpdateCache();
                    break;
                
                case SettingsTypeContract.AssetPair:
                    //AssetPair change handled in AssetPairProjection
                    break;
                
                case SettingsTypeContract.TradingCondition:
                    _tradingConditionsManager.InitTradingConditions();
                    break;
                
                case SettingsTypeContract.TradingInstrument:
                    _tradingInstrumentsManager.UpdateTradingInstrumentsCache();
                    break;
                
                case SettingsTypeContract.TradingRoute:
                    _matchingEngineRoutesManager.UpdateRoutesCacheAsync();
                    break;
                
                default:
                    throw new NotImplementedException($"Type {message.SettingsType} is not supported");
            }
            
            return Task.CompletedTask;
        }
    }
}