using MarginTrading.Backend.Core;

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

        public decimal GetQuoteRateForBaseAsset(string accountAssetId, string instrument, string legalEntity)
        {
            var asset = _assetPairsCache.GetAssetPairById(instrument);

            var baseAssetId = asset.BaseAssetId;

            if (accountAssetId == baseAssetId)
            {
                return 1;
            }

            var inst = _assetPairsCache.FindAssetPair(baseAssetId, accountAssetId, legalEntity);
            var quote = _quoteCacheService.GetQuote(inst.Id);

            if (inst.BaseAssetId == baseAssetId)
            {
                return quote.Ask;
            }

            return 1.0M / quote.Bid;
        }

        public decimal GetQuoteRateForQuoteAsset(string accountAssetId, string assetPairId, string legalEntity)
        {
            var assetPair = _assetPairsCache.GetAssetPairById(assetPairId);

            var quoteAssetId = assetPair.QuoteAssetId;

            if (accountAssetId == quoteAssetId)
            {
                return 1;
            }

            var crossAssetPair = _assetPairsCache.FindAssetPair(quoteAssetId, accountAssetId, legalEntity);
            var quote = _quoteCacheService.GetQuote(crossAssetPair.Id);

            if (crossAssetPair.BaseAssetId == quoteAssetId)
            {
                return quote.Bid;
            }

            return 1.0M / quote.Ask;
        }

        public decimal GetVolumeInAccountAsset(OrderDirection direction, string accountAssetId, string instrument,
            decimal volume, string legalEntity)
        {
            var inst = _assetPairsCache.GetAssetPairById(instrument);
            var instrumentQuote = _quoteCacheService.GetQuote(instrument);

            var rate = GetQuoteRateForQuoteAsset(accountAssetId, inst.Id, legalEntity);
            var price = instrumentQuote.GetPriceForOrderType(direction == OrderDirection.Buy
                ? OrderDirection.Sell
                : OrderDirection.Buy);

            return volume * rate * price;
        }
    }
}