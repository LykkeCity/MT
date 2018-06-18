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
            bool metricIsPositive = true)
        {
            var assetPair = _assetPairsCache.GetAssetPairById(assetPairId);
            
            if (accountAssetId == assetPair.BaseAssetId)
                return 1;
            
            //two step transform: base -> quote from QuoteCache, quote -> account from FxCache
            var assetPairQuote = _quoteCacheService.GetQuote(assetPairId);
            if (assetPair.QuoteAssetId == accountAssetId)
                return metricIsPositive ? assetPairQuote.Ask : assetPairQuote.Bid;
            //todo think... what if there's no Fx pair.. maybe we should use trade quote in such case?
            var assetPairSubst =
                _assetPairsCache.FindAssetPair(assetPair.QuoteAssetId, accountAssetId, legalEntity);
            var assetPairSubstQuote = _fxRateCacheService.GetQuote(assetPairSubst.Id);

            var rate = metricIsPositive
                ? assetPairSubst.BaseAssetId == assetPair.QuoteAssetId
                    ? assetPairSubstQuote.Ask * assetPairQuote.Ask
                    : (1 / assetPairSubstQuote.Bid) * assetPairQuote.Ask
                : assetPairSubst.BaseAssetId == assetPair.QuoteAssetId
                    ? assetPairSubstQuote.Bid * assetPairQuote.Bid
                    : (1 / assetPairSubstQuote.Ask) * assetPairQuote.Bid;

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
                    ? _quoteCacheService.GetQuote(assetPairSubst.Id).Ask
                    : 1 / _quoteCacheService.GetQuote(assetPairSubst.Id).Bid
                : assetPairSubst.BaseAssetId == assetPair.QuoteAssetId
                    ? _quoteCacheService.GetQuote(assetPairSubst.Id).Bid
                    : 1 / _quoteCacheService.GetQuote(assetPairSubst.Id).Ask;
            
            return rate;
        }
    }
}