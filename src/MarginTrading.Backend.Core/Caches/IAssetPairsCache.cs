using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public interface IAssetPairsCache
    {
        IAssetPair GetAssetPairById(string assetPairId);
        /// <summary>
        /// Tries to get an asset pair, if it is not found null is returned.
        /// </summary>
        /// <param name="assetPairId"></param>
        /// <returns></returns>
        [CanBeNull] IAssetPair GetAssetPairByIdOrDefault(string assetPairId);
        IEnumerable<IAssetPair> GetAll();
        IAssetPair FindAssetPair(string asset1, string asset2, string legalEntity);
        /// <summary>
        /// Tries to get an asset pair constructed as [instrument.baseAsset, substAsset] or reversed.
        /// If such pairs are not found returns false.
        /// </summary>
        /// <param name="substAsset"></param>
        /// <param name="instrument"></param>
        /// <param name="legalEntity"></param>
        /// <param name="assetPair"></param>
        /// <returns></returns>
        bool TryGetAssetPairQuoteSubst(string substAsset, string instrument, string legalEntity, 
            out IAssetPair assetPair);
        ImmutableHashSet<string> GetAllIds();
    }
}