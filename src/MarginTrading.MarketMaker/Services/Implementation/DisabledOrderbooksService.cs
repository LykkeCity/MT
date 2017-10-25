using System;
using System.Collections.Immutable;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class DisabledOrderbooksService : IDisabledOrderbooksService
    {
        private readonly IPriceCalcSettingsService _priceCalcSettingsService;

        public DisabledOrderbooksService(IPriceCalcSettingsService priceCalcSettingsService)
        {
            _priceCalcSettingsService = priceCalcSettingsService;
        }

        public ImmutableHashSet<string> GetDisabledExchanges(string assetPairId)
        {
            return _priceCalcSettingsService.GetDisabledExchanges(assetPairId);
        }

        public void Disable(string assetPairId, ImmutableHashSet<string> exchanges)
        {
            _priceCalcSettingsService.ChangeExchangesTemporarilyDisabled(assetPairId, exchanges, true);
        }
    }
}
