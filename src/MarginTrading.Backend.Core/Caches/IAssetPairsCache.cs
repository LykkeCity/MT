using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public interface IAssetPairsCache
    {
        IAssetPair GetAssetPairById(string assetPairId);
        IEnumerable<IAssetPair> GetAll();
        IAssetPair TryFindAssetPair(string asset1, string asset2, string legalEntity);
        IAssetPair FindAssetPair(string asset1, string asset2, string legalEntity);
        bool TryGetAssetPairQuoteSubstWithResersed(string substAsset, string instrument, string legalEntity, 
            out IAssetPair assetPair);
        ImmutableHashSet<string> GetAllIds();
        [CanBeNull] IAssetPair TryGetAssetPairById(string assetPairId);
    }
}