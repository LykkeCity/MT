using System;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class OutdatedOrderbooksService : IOutdatedOrderbooksService
    {
        private readonly IPriceCalcSettingsService _priceCalcSettingsService;

        public OutdatedOrderbooksService(IPriceCalcSettingsService priceCalcSettingsService)
        {
            _priceCalcSettingsService = priceCalcSettingsService;
        }

        public bool IsOutdated(ExternalOrderbook orderbook, DateTime now)
        {
            var age = now - orderbook.LastUpdatedTime;
            var threshold = _priceCalcSettingsService.GetOrderbookOutdatingThreshold(orderbook.AssetPairId, orderbook.ExchangeName, now);
            return age > threshold;
        }
    }
}