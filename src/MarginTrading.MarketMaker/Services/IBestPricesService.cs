using System.Collections.Generic;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services
{
    public interface IBestPricesService
    {
        BestPrices Calc(ExternalOrderbook orderbook);
        
        [Pure]
        IReadOnlyDictionary<(string AssetPairId, string Exchange), BestPrices> GetLastCalculated();
    }
}