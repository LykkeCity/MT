using System.Collections.Immutable;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class HedgingPreferenceService : IHedgingPreferenceService
    {
        private readonly IPriceCalcSettingsService _priceCalcSettingsService;

        public HedgingPreferenceService(IPriceCalcSettingsService priceCalcSettingsService)
        {
            _priceCalcSettingsService = priceCalcSettingsService;
        }

        public ImmutableDictionary<string, decimal> Get(string assetPairId)
        {
            // for now - get from settings
            return _priceCalcSettingsService.GetHedgingPreferences(assetPairId);
        }
    }
}