using System.Collections.Generic;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.AssetPairs
{
    internal interface IAssetPairsInitializableCache : IAssetPairsCache
    {
        void InitPairsCache(Dictionary<string, IAssetPair> instruments);
    }
}