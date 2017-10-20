using System;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.MarketMaker.HelperServices;
using MarginTrading.MarketMaker.Messages;
using MarginTrading.MarketMaker.Settings;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    internal class BrokerService : IBrokerService
    {
        private readonly ILog _logger;
        private readonly IMarketMakerService _marketMakerService;
        private readonly IRabbitMqService _rabbitMqService;
        private readonly IReloadingManager<MarginTradingMarketMakerSettings> _settings;

        public BrokerService(ILog logger, IReloadingManager<MarginTradingMarketMakerSettings> settings, IMarketMakerService marketMakerService,
            IRabbitMqService rabbitMqService)
        {
            _logger = logger;
            _settings = settings;
            _marketMakerService = marketMakerService;
            _rabbitMqService = rabbitMqService;
        }

        public void Run()
        {
            _logger.WriteInfoAsync(Startup.ServiceName, null, null, "Starting broker").Wait();
            try
            {
                _rabbitMqService.Subscribe<ExternalExchangeOrderbookMessage>(_settings.Nested(s => s.RabbitMq.Consumers.FiatOrderbooks), false,
                    _marketMakerService.ProcessNewExternalOrderbookAsync);
                _rabbitMqService.Subscribe<ExternalExchangeOrderbookMessage>(_settings.Nested(s => s.RabbitMq.Consumers.CryptoOrderbooks), false,
                    _marketMakerService.ProcessNewExternalOrderbookAsync);
                _rabbitMqService.Subscribe<SpotOrderbookMessage>(_settings.Nested(s => s.RabbitMq.Consumers.SpotOrderBook), false,
                    _marketMakerService.ProcessNewSpotOrderBookDataAsync);
            }
            catch (Exception ex)
            {
                _logger.WriteErrorAsync(Startup.ServiceName, "Broker.Run", null, ex).Wait();
            }
        }
    }
}