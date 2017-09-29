using System.Collections.Generic;

namespace MarginTrading.Core
{
    public interface IAssetPairsCache
    {
        IAssetPair GetAssetPairById(string assetPairId);
        IEnumerable<IAssetPair> GetAll();
        IAssetPair FindInstrument(string asset1, string asset2);
    }
}