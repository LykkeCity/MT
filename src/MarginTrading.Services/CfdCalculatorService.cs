using MarginTrading.Core;

namespace MarginTrading.Services
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

        public double GetQuoteRateForBaseAsset(string accountAssetId, string instrument)
        {
            var asset = _assetPairsCache.GetAssetPairById(instrument);

            string baseAssetId = asset.BaseAssetId;

            if (accountAssetId == baseAssetId)
            {
                return 1;
            }

            var inst = _assetPairsCache.FindInstrument(baseAssetId, accountAssetId);
            var quote = _quoteCacheService.GetQuote(inst.Id);

            if (inst.BaseAssetId == baseAssetId)
            {
                return quote.Bid;
            }

            return 1.0 / quote.Ask;
        }

        public double GetQuoteRateForQuoteAsset(string accountAssetId, string instrument)
        {
            var asset = _assetPairsCache.GetAssetPairById(instrument);

            string quoteAssetId = asset.QuoteAssetId;

            if (accountAssetId == quoteAssetId)
            {
                return 1;
            }

            var inst = _assetPairsCache.FindInstrument(quoteAssetId, accountAssetId);
            var quote = _quoteCacheService.GetQuote(inst.Id);

            if (inst.BaseAssetId == quoteAssetId)
            {
                return quote.Bid;
            }

            return 1.0 / quote.Ask;
        }

        public double GetVolumeInAccountAsset(OrderDirection direction, string accountAssetId, string instrument, double volume)
        {
            var inst = _assetPairsCache.GetAssetPairById(instrument);
            var instrumentQuote = _quoteCacheService.GetQuote(instrument);

            double rate = GetQuoteRateForQuoteAsset(accountAssetId, inst.Id);
            double price = instrumentQuote.GetPriceForOrderType(direction == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy);

            return volume * rate * price;
        }
    }
}
