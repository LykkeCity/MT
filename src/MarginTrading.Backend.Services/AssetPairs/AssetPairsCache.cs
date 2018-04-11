using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Services.Infrastructure;
using MoreLinq;
using AssetPairKey = System.ValueTuple<string, string, string>;

namespace MarginTrading.Backend.Services.AssetPairs
{
    /// <summary>
    /// Cashes data about assets in the backend app.
    /// </summary>
    /// <remarks>
    /// Note this type is thread-safe, though it has no synchronization.
    /// This is due to the fact that the <see cref="_assetPairs"/> dictionary
    /// is used as read-only: never updated, only reference-assigned.
    /// Their contents are also readonly.
    /// </remarks>
    public class AssetPairsCache : IAssetPairsInitializableCache
    {
        private IReadOnlyDictionary<string, IAssetPair> _assetPairs = 
            ImmutableSortedDictionary<string, IAssetPair>.Empty;

        private readonly ICachedCalculation<IReadOnlyDictionary<AssetPairKey, IAssetPair>> _assetPairsByAssets;
        private readonly ICachedCalculation<ImmutableHashSet<string>> _assetPairsIds;

        public AssetPairsCache()
        {
            _assetPairsByAssets = GetAssetPairsByAssetsCache();
            _assetPairsIds = Calculate.Cached(() => _assetPairs, ReferenceEquals, p => p.Keys.ToImmutableHashSet());
        }

        public IAssetPair GetAssetPairById(string assetPairId)
        {
            return _assetPairs.TryGetValue(assetPairId, out var result)
                ? result
                : throw new AssetPairNotFoundException(assetPairId,
                    string.Format(MtMessages.InstrumentNotFoundInCache, assetPairId));
        }

        public IAssetPair TryGetAssetPairById(string assetPairId)
        {
            return _assetPairs.GetValueOrDefault(assetPairId);
        }

        public IEnumerable<IAssetPair> GetAll()
        {
            return _assetPairs.Values;
        }

        public ImmutableHashSet<string> GetAllIds()
        {
            return _assetPairsIds.Get();
        }
        
        public IAssetPair TryFindAssetPair(string asset1, string asset2, string legalEntity)
        {
            var key = GetAssetPairKey(asset1, asset2, legalEntity);
            
            return _assetPairsByAssets.Get().TryGetValue(key, out var result) ? result : null;
        }

        public IAssetPair FindAssetPair(string asset1, string asset2, string legalEntity)
        {
            var key = GetAssetPairKey(asset1, asset2, legalEntity);
            
            if (_assetPairsByAssets.Get().TryGetValue(key, out var result))
                return result;

            throw new InstrumentByAssetsNotFoundException(asset1, asset2,
                string.Format(MtMessages.InstrumentWithAssetsNotFound, asset1, asset2));
        }

        void IAssetPairsInitializableCache.InitPairsCache(Dictionary<string, IAssetPair> instruments)
        {
            _assetPairs = instruments;
        }

        private ICachedCalculation<IReadOnlyDictionary<AssetPairKey, IAssetPair>> GetAssetPairsByAssetsCache()
        {
            return Calculate.Cached(() => _assetPairs, ReferenceEquals,
                pairs => pairs.Values.SelectMany(p => new []
                {
                    (GetAssetPairKey(p.BaseAssetId, p.QuoteAssetId, p.LegalEntity), p),
                    (GetAssetPairKey(p.QuoteAssetId, p.BaseAssetId, p.LegalEntity), p),
                }).ToDictionary());
        }

        private static AssetPairKey GetAssetPairKey(string asset1, string asset2, string legalEntity)
        {
            return (asset1, asset2, legalEntity);
        }
    }
}