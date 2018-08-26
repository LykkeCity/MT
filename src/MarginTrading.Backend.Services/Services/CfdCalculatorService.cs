using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Services;

namespace MarginTrading.Backend.Services
{
    public class CfdCalculatorService : ICfdCalculatorService
    {
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IFxRateCacheService _fxRateCacheService;
        private readonly IQuoteCacheService _quoteCacheService;

        public CfdCalculatorService(
            IAssetPairsCache assetPairsCache,
            IFxRateCacheService fxRateCacheService,
            IQuoteCacheService quoteCacheService)
        {
            _assetPairsCache = assetPairsCache;
            _fxRateCacheService = fxRateCacheService;
            _quoteCacheService = quoteCacheService;
        }

        public decimal GetQuoteRateForBaseAsset(string accountAssetId, string assetPairId, string legalEntity, 
            bool isBuy)
        {
            var assetPair = _assetPairsCache.GetAssetPairById(assetPairId);
            
            // two step transform: base -> quote from QuoteCache, quote -> account from FxCache
            // if accountAssetId == assetPair.BaseAssetId, rate != 1, because trading and fx rates can be different
            
            var assetPairQuote = _quoteCacheService.GetQuote(assetPairId);
            var tradingRate = isBuy ? assetPairQuote.Ask : assetPairQuote.Bid;

            if (assetPair.QuoteAssetId == accountAssetId)
                return tradingRate;
            
            var fxPair =
                _assetPairsCache.FindAssetPair(assetPair.QuoteAssetId, accountAssetId, legalEntity);
            var fxQuote = _fxRateCacheService.GetQuote(fxPair.Id);

            var rate = fxPair.BaseAssetId == assetPair.QuoteAssetId
                ? fxQuote.Ask * tradingRate
                : 1 / fxQuote.Bid * tradingRate;

            return rate;
        }

        public decimal GetQuoteRateForQuoteAsset(string accountAssetId, string assetPairId, string legalEntity, 
            bool metricIsPositive = true)
        {
            var assetPair = _assetPairsCache.GetAssetPairById(assetPairId);
            
            if (accountAssetId == assetPair.QuoteAssetId)
                return 1;

            var assetPairSubst = _assetPairsCache.FindAssetPair(assetPair.QuoteAssetId, accountAssetId, legalEntity);
           
            var rate = metricIsPositive
                ? assetPairSubst.BaseAssetId == assetPair.QuoteAssetId
                    ? _fxRateCacheService.GetQuote(assetPairSubst.Id).Ask
                    : 1 / _fxRateCacheService.GetQuote(assetPairSubst.Id).Bid
                : assetPairSubst.BaseAssetId == assetPair.QuoteAssetId
                    ? _fxRateCacheService.GetQuote(assetPairSubst.Id).Bid
                    : 1 / _fxRateCacheService.GetQuote(assetPairSubst.Id).Ask;
            
            return rate;
        }
    }
}