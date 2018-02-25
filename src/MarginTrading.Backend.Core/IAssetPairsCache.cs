using System.Collections.Generic;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public interface IAssetPairsCache
    {
        IAssetPair GetAssetPairById(string assetPairId);
        IEnumerable<IAssetPair> GetAll();
        IAssetPair FindAssetPair(string asset1, string asset2);
        HashSet<string> GetAllIds();
        [CanBeNull] IAssetPair TryGetAssetPairById(string assetPairId);
        [CanBeNull] IAssetPairSettings GetAssetPairSettings(string assetPairId);
    }
}