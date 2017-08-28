using System.Collections.Generic;

namespace MarginTrading.Core
{
    public interface IAssetPairsCache
    {
        IMarginTradingAssetPair GetAssetPairById(string assetPairId);
        IEnumerable<IMarginTradingAssetPair> GetAll();
        IMarginTradingAssetPair FindInstrument(string asset1, string asset2);
    }
}