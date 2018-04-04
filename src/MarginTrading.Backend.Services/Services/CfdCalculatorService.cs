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

        public decimal GetFplRate(string accountAssetId, string instrumentId, string legalEntity, bool fplSign)
        {
            var assetPair = _assetPairsCache.GetAssetPairById(instrumentId);
            
            if (accountAssetId == assetPair.QuoteAssetId)
                return 1;

            var assetPairQuoteAccount = _assetPairsCache.TryFindAssetPairStraight(assetPair.QuoteAssetId, accountAssetId, legalEntity);
            IAssetPair assetPairAccountQuote = null;
            if (assetPairQuoteAccount == null)
                assetPairAccountQuote = _assetPairsCache.TryFindAssetPairStraight(accountAssetId, assetPair.QuoteAssetId, legalEntity);

            var rate = fplSign
                ? assetPairQuoteAccount != null
                    ? _quoteCacheService.GetQuote(assetPairQuoteAccount.Id).Ask
                    : 1 / _quoteCacheService.GetQuote(assetPairAccountQuote.Id).Bid
                : assetPairQuoteAccount != null
                    ? _quoteCacheService.GetQuote(assetPairQuoteAccount.Id).Bid
                    : 1 / _quoteCacheService.GetQuote(assetPairAccountQuote.Id).Ask;
            
            return rate;
        }

        public decimal GetSwapRate(string accountAssetId, string instrumentId, string legalEntity, bool swapSign)
        {
            var assetPair = _assetPairsCache.GetAssetPairById(instrumentId);
            
            if (accountAssetId == assetPair.BaseAssetId)
                return 1;

            var assetPairBaseAccount = _assetPairsCache.TryFindAssetPairStraight(assetPair.BaseAssetId, accountAssetId, legalEntity);
            IAssetPair assetPairAccountBase = null;
            if (assetPairBaseAccount == null)
                assetPairAccountBase = _assetPairsCache.TryFindAssetPairStraight(accountAssetId, assetPair.BaseAssetId, legalEntity);

            var rate = swapSign
                ? assetPairBaseAccount != null
                    ? _quoteCacheService.GetQuote(assetPairBaseAccount.Id).Ask
                    : 1 / _quoteCacheService.GetQuote(assetPairAccountBase.Id).Bid
                : assetPairBaseAccount != null
                    ? _quoteCacheService.GetQuote(assetPairBaseAccount.Id).Bid
                    : 1 / _quoteCacheService.GetQuote(assetPairAccountBase.Id).Ask;
            
            return rate;
        }

        public decimal GetMarginRate(string accountAssetId, string instrumentId, string legalEntity)
        {
            return GetSwapRate(accountAssetId, instrumentId, legalEntity, true);
        }

        public decimal GetVolumeInAccountAsset(OrderDirection direction, string accountAssetId, string instrument,
            decimal volume, string legalEntity)
        {
            var inst = _assetPairsCache.GetAssetPairById(instrument);
            var instrumentQuote = _quoteCacheService.GetQuote(instrument);

            var rate = GetFplRate(accountAssetId, inst.Id, legalEntity, direction == OrderDirection.Buy);//GetQuoteRateForQuoteAsset(accountAssetId, inst.Id, legalEntity);
            var price = instrumentQuote.GetPriceForOrderType(direction == OrderDirection.Buy
                ? OrderDirection.Sell
                : OrderDirection.Buy);

            return volume * rate * price;
        }
    }
}