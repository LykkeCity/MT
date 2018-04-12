using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Services.AssetPairs;

namespace MarginTrading.Backend.Services
{
    public class CfdCalculatorService : ICfdCalculatorService
    {
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IQuoteCacheService _quoteCacheService;

        public CfdCalculatorService(
            IAssetPairsCache assetPairsCache,
            IQuoteCacheService quoteCacheService)
        {
            _assetPairsCache = assetPairsCache;
            _quoteCacheService = quoteCacheService;
        }

        public decimal GetQuoteRateForBaseAsset(string accountAssetId, string assetPairId, string legalEntity, 
            bool metricIsPositive = true)
        {
            var assetPair = _assetPairsCache.GetAssetPairById(assetPairId);
            
            if (accountAssetId == assetPair.BaseAssetId)
                return 1;

            var assetPairSubst = _assetPairsCache.FindAssetPair(assetPair.BaseAssetId, accountAssetId, legalEntity);

            var rate = metricIsPositive
                ? assetPairSubst.BaseAssetId == assetPair.BaseAssetId
                    ? _quoteCacheService.GetQuote(assetPairSubst.Id).Ask
                    : 1 / _quoteCacheService.GetQuote(assetPairSubst.Id).Bid
                : assetPairSubst.BaseAssetId == assetPair.BaseAssetId
                    ? _quoteCacheService.GetQuote(assetPairSubst.Id).Bid
                    : 1 / _quoteCacheService.GetQuote(assetPairSubst.Id).Ask;
            
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