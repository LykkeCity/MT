using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
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
            bool useAsk)
        {
            var assetPair = _assetPairsCache.GetAssetPairById(assetPairId);
            
            // two step transform: base -> quote from QuoteCache, quote -> account from FxCache
            // if accountAssetId == assetPair.BaseAssetId, rate != 1, because trading and fx rates can be different
            
            var assetPairQuote = _quoteCacheService.GetQuote(assetPairId);
            var tradingRate = useAsk ? assetPairQuote.Ask : assetPairQuote.Bid;

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

            var fxPair =
                _assetPairsCache.FindAssetPair(assetPair.QuoteAssetId, accountAssetId, legalEntity);
            var fxQuote = _fxRateCacheService.GetQuote(fxPair.Id);
           
            var rate = metricIsPositive
                ? fxPair.BaseAssetId == assetPair.QuoteAssetId
                    ? fxQuote.Ask
                    : 1 / fxQuote.Bid
                : fxPair.BaseAssetId == assetPair.QuoteAssetId
                    ? fxQuote.Bid
                    : 1 / fxQuote.Ask;
            
            return rate;
        }

        public decimal GetQuoteRateForQuoteAsset(InstrumentBidAskPair quote, FxToAssetPairDirection direction, 
            bool metricIsPositive = true)
        {
            return metricIsPositive
                ? direction == FxToAssetPairDirection.Straight
                    ? quote.Ask
                    : 1 / quote.Bid
                : direction == FxToAssetPairDirection.Straight
                    ? quote.Bid
                    : 1 / quote.Ask;
        }

        public (string id, FxToAssetPairDirection direction) GetFxAssetPairIdAndDirection(string accountAssetId,
            string assetPairId, string legalEntity)
        {
            var assetPair = _assetPairsCache.GetAssetPairById(assetPairId);
            
            if (accountAssetId == assetPair.QuoteAssetId)
                return (LykkeConstants.SymmetricAssetPair, FxToAssetPairDirection.Straight);

            var fxAssetPair = _assetPairsCache.FindAssetPair(assetPair.QuoteAssetId, accountAssetId, legalEntity);
            var direction = assetPair.QuoteAssetId == fxAssetPair.BaseAssetId
                ? FxToAssetPairDirection.Straight
                : FxToAssetPairDirection.Reverse;

            return (fxAssetPair.Id, direction);
        }
    }
}