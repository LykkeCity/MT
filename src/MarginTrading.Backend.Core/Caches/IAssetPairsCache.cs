using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public interface IAssetPairsCache
    {
        IAssetPair GetAssetPairById(string assetPairId);
        IEnumerable<IAssetPair> GetAll();
        IAssetPair FindAssetPair(string asset1, string asset2, string legalEntity);
        bool TryGetAssetPairById(string assetPairId, out IAssetPair assetPair);
        bool TryGetAssetPairQuoteSubstWithResersed(string substAsset, string instrument, string legalEntity, 
            out IAssetPair assetPair);
        ImmutableHashSet<string> GetAllIds();
    }
}