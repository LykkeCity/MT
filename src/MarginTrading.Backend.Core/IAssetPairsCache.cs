using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
    public interface IAssetPairsCache
    {
        IAssetPair GetAssetPairById(string assetPairId);
        IEnumerable<IAssetPair> GetAll();
        IAssetPair FindAssetPair(string asset1, string asset2);
        HashSet<string> GetAllIds();
        bool TryGetAssetPairById(string assetPairId, out IAssetPair assetPair);
        bool TryGetAssetPairQuoteSubstWithResersed(string substAsset, string instrument, out IAssetPair assetPair);
    }
}