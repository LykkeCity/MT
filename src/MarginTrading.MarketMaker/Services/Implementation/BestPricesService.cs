using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class BestPricesService : IBestPricesService
    {
        private readonly ReadWriteLockedDictionary<(string, string), BestPrices> _lastBestPrices =
            new ReadWriteLockedDictionary<(string, string), BestPrices>();

        public BestPrices Calc(ExternalOrderbook orderbook)
        {
            var bestPrices = new BestPrices(
                orderbook.Bids.Max(b => b.Price),
                orderbook.Asks.Min(b => b.Price));
            _lastBestPrices[(orderbook.AssetPairId, orderbook.ExchangeName)] = bestPrices;
            return bestPrices;
        }

        [Pure]
        public IReadOnlyDictionary<(string AssetPairId, string Exchange), BestPrices> GetLastCalculated()
        {
            return _lastBestPrices;
        }
    }
}
