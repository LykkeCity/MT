using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Messages;

namespace MarginTrading.Backend.Services.AssetPairs
{
    /// <summary>
    /// Cashes data about assets in the backend app.
    /// </summary>
    /// <remarks>
    /// Note this type is thread-safe, though it has no synchronization.
    /// This is due to the fact that <see cref="_assetPairs"/> & <see cref="_assetPairSettings"/> dictionaries
    /// are used as read-only: never updated, only reference-assigned.
    /// Their contents are also readonly.
    /// </remarks>
    public class AssetPairsCache : IAssetPairsInitializableCache
    {
        private IReadOnlyDictionary<string, IAssetPair> _assetPairs =
            ImmutableSortedDictionary<string, IAssetPair>.Empty;

        private IReadOnlyDictionary<string, IAssetPairSettings> _assetPairSettings =
            ImmutableSortedDictionary<string, IAssetPairSettings>.Empty;

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

        public IAssetPairSettings GetAssetPairSettings(string assetPairId)
        {
            return _assetPairSettings.GetValueOrDefault(assetPairId);
        }

        public IEnumerable<IAssetPair> GetAll()
        {
            return _assetPairs.Values;
        }

        public HashSet<string> GetAllIds()
        {
            return _assetPairs.Keys.ToHashSet();
        }

        public IAssetPair FindAssetPair(string asset1, string asset2)
        {
            // todo: search pairs using baseAssetId & quotingAssetId
            if (_assetPairs.TryGetValue(GetAssetPairId(asset1, asset2), out var result))
                return result;

            if (_assetPairs.TryGetValue(GetAssetPairId(asset2, asset1), out result))
                return result;

            throw new InstrumentByAssetsNotFoundException(asset1, asset2,
                string.Format(MtMessages.InstrumentWithAssetsNotFound, asset1, asset2));
        }

        void IAssetPairsInitializableCache.InitInstrumentsCache(Dictionary<string, IAssetPair> instruments)
        {
            _assetPairs = instruments;
        }

        void IAssetPairsInitializableCache.InitAssetPairSettingsCache(Dictionary<string, IAssetPairSettings> assetPairSettings)
        {
            _assetPairSettings = assetPairSettings;
        }

        private static string GetAssetPairId(string asset1, string asset2)
        {
            return $"{asset1}{asset2}";
        }
    }
}