// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.AssetPairs
{
    public interface IAssetPairsInitializableCache : IAssetPairsCache
    {
        void InitPairsCache(Dictionary<string, IAssetPair> instruments);
    }
}