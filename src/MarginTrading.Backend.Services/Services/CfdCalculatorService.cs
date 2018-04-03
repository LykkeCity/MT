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

        public decimal GetQuoteRateForBaseAsset(string accountAssetId, string instrument)
        {
            var asset = _assetPairsCache.GetAssetPairById(instrument);

            var baseAssetId = asset.BaseAssetId;

            if (accountAssetId == baseAssetId)
            {
                return 1;
            }

            var inst = _assetPairsCache.FindAssetPair(baseAssetId, accountAssetId);
            var quote = _quoteCacheService.GetQuote(inst.Id);

            if (inst.BaseAssetId == baseAssetId)
            {
                return quote.Ask;
            }

            return 1.0M / quote.Bid;
        }

        public decimal GetQuoteRateForQuoteAsset(string accountAssetId, string assetPairId)
        {
            var assetPair = _assetPairsCache.GetAssetPairById(assetPairId);

            var quoteAssetId = assetPair.QuoteAssetId;

            if (accountAssetId == quoteAssetId)
            {
                return 1;
            }

            var crossAssetPair = _assetPairsCache.FindAssetPair(quoteAssetId, accountAssetId);
            var quote = _quoteCacheService.GetQuote(crossAssetPair.Id);

            if (crossAssetPair.BaseAssetId == quoteAssetId)
            {
                return quote.Bid;
            }

            return 1.0M / quote.Ask;
        }

        public decimal GetFplRate(string accountAssetId, string instrumentId, bool fplSign)
        {
            var assetPair = _assetPairsCache.GetAssetPairById(instrumentId);
            
            if (accountAssetId == assetPair.QuoteAssetId)
                return 1;

            var assetPairQuoteAccount = _assetPairsCache
                .TryGetAssetPairById(AssetPairsCache.GetAssetPairKey(assetPair.QuoteAssetId, accountAssetId));

            var rate = fplSign
                ? assetPairQuoteAccount != null
                    ? _quoteCacheService
                        .GetQuote(AssetPairsCache.GetAssetPairKey(assetPair.QuoteAssetId, accountAssetId)).Ask
                    : 1 / _quoteCacheService
                          .GetQuote(AssetPairsCache.GetAssetPairKey(accountAssetId, assetPair.QuoteAssetId)).Bid
                : assetPairQuoteAccount != null
                    ? _quoteCacheService
                        .GetQuote(AssetPairsCache.GetAssetPairKey(assetPair.QuoteAssetId, accountAssetId)).Bid
                    : 1 / _quoteCacheService
                          .GetQuote(AssetPairsCache.GetAssetPairKey(accountAssetId, assetPair.QuoteAssetId)).Ask;
            
            return rate;
        }

        public decimal GetSwapRate(string accountAssetId, string instrumentId, bool swapSign)
        {
            var assetPair = _assetPairsCache.GetAssetPairById(instrumentId);
            
            if (accountAssetId == assetPair.QuoteAssetId)
                return 1;

            var assetPairQuoteAccount = _assetPairsCache
                .TryGetAssetPairById(AssetPairsCache.GetAssetPairKey(assetPair.BaseAssetId, accountAssetId));

            var rate = swapSign
                ? assetPairQuoteAccount != null
                    ? _quoteCacheService
                        .GetQuote(AssetPairsCache.GetAssetPairKey(assetPair.BaseAssetId, accountAssetId)).Ask
                    : 1 / _quoteCacheService
                          .GetQuote(AssetPairsCache.GetAssetPairKey(accountAssetId, assetPair.BaseAssetId)).Bid
                : assetPairQuoteAccount != null
                    ? _quoteCacheService
                        .GetQuote(AssetPairsCache.GetAssetPairKey(assetPair.BaseAssetId, accountAssetId)).Bid
                    : 1 / _quoteCacheService
                          .GetQuote(AssetPairsCache.GetAssetPairKey(accountAssetId, assetPair.BaseAssetId)).Ask;
            
            return rate;
        }

        public decimal GetMarginRate(string accountAssetId, string instrumentId)
        {
            return GetSwapRate(accountAssetId, instrumentId, true);
        }

        public decimal GetVolumeInAccountAsset(OrderDirection direction, string accountAssetId, string instrument, decimal volume)
        {
            var inst = _assetPairsCache.GetAssetPairById(instrument);
            var instrumentQuote = _quoteCacheService.GetQuote(instrument);

            var rate = GetQuoteRateForQuoteAsset(accountAssetId, inst.Id);
            var price = instrumentQuote.GetPriceForOrderType(direction == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy);

            return volume * rate * price;
        }
    }
}
