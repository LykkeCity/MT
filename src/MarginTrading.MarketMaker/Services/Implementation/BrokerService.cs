using System;
using Common.Log;
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
        private readonly MarginTradingMarketMakerSettings _settings;

        public BrokerService(ILog logger, MarginTradingMarketMakerSettings settings, IMarketMakerService marketMakerService,
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
                _rabbitMqService.Subscribe<BestBidAskMessage>(_settings.RabbitMq.IcmQuotesConnectionSettings, false,
                    _marketMakerService.ProcessNewIcmBestBidAskAsync);
                _rabbitMqService.Subscribe<OrderBookMessage>(_settings.RabbitMq.SpotOrderBookConnectionSettings, false,
                    _marketMakerService.ProcessNewSpotOrderBookDataAsync);
            }
            catch (Exception ex)
            {
                _logger.WriteErrorAsync(Startup.ServiceName, "Broker.Run", null, ex).Wait();
            }
        }
    }
}