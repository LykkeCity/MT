using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class CfdCalculatorService : ICfdCalculatorService
    {
        private readonly IInstrumentsCache _instrumentsCache;
        private readonly IQuoteCacheService _quoteCacheService;

        public CfdCalculatorService(
            IInstrumentsCache instrumentsCache,
            IQuoteCacheService quoteCacheService)
        {
            _instrumentsCache = instrumentsCache;
            _quoteCacheService = quoteCacheService;
        }

        public double GetQuoteRateForBaseAsset(string accountAssetId, string instrument)
        {
            var asset = _instrumentsCache.GetInstrumentById(instrument);

            string baseAssetId = asset.BaseAssetId;

            if (accountAssetId == baseAssetId)
            {
                return 1;
            }

            var inst = _instrumentsCache.FindInstrument(baseAssetId, accountAssetId);
            var quote = _quoteCacheService.GetQuote(inst.Id);

            if (inst.BaseAssetId == baseAssetId)
            {
                return quote.Bid;
            }

            return 1.0 / quote.Ask;
        }

        public double GetQuoteRateForQuoteAsset(string accountAssetId, string instrument)
        {
            var asset = _instrumentsCache.GetInstrumentById(instrument);

            string quoteAssetId = asset.QuoteAssetId;

            if (accountAssetId == quoteAssetId)
            {
                return 1;
            }

            var inst = _instrumentsCache.FindInstrument(quoteAssetId, accountAssetId);
            var quote = _quoteCacheService.GetQuote(inst.Id);

            if (inst.BaseAssetId == quoteAssetId)
            {
                return quote.Bid;
            }

            return 1.0 / quote.Ask;
        }

        public double GetVolumeInAccountAsset(OrderDirection direction, string accountAssetId, string instrument, double volume)
        {
            var inst = _instrumentsCache.GetInstrumentById(instrument);
            var instrumentQuote = _quoteCacheService.GetQuote(instrument);

            double rate = GetQuoteRateForQuoteAsset(accountAssetId, inst.Id);
            double price = instrumentQuote.GetPriceForOrderType(direction == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy);

            return volume * rate * price;
        }
    }
}
