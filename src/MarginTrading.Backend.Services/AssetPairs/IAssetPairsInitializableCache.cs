using System.Collections.Generic;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.AssetPairs
{
    internal interface IAssetPairsInitializableCache : IAssetPairsCache
    {
        void InitInstrumentsCache(Dictionary<string, IAssetPair> instruments);
        void InitAssetPairSettingsCache(Dictionary<string, IAssetPairSettings> assetPairSettings);
    }
}