// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Autofac;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Services;
using MarginTrading.AssetService.Contracts;
using MarginTrading.AssetService.Contracts.AssetPair;

namespace MarginTrading.Backend.Services.AssetPairs
{
    [UsedImplicitly]
    internal class AssetPairsManager : IStartable, IAssetPairsManager
    {
        private static readonly object InitAssetPairsLock = new object();

        private readonly IAssetPairsInitializableCache _assetPairsCache;
        private readonly IAssetPairsApi _assetPairs;
        private readonly IConvertService _convertService;

        public AssetPairsManager(IAssetPairsInitializableCache assetPairsCache,
            IAssetPairsApi assetPairs,
            IConvertService convertService)
        {
            _assetPairsCache = assetPairsCache;
            _assetPairs = assetPairs;
            _convertService = convertService;
        }

        public void Start()
        {
            InitAssetPairs();
        }

        public void InitAssetPairs()
        {
            lock (InitAssetPairsLock)
            {
                var pairs = _assetPairs.List().GetAwaiter().GetResult()
                    .ToDictionary(a => a.Id,
                        s => (IAssetPair) _convertService.Convert<AssetPairContract, AssetPair>(s));
                _assetPairsCache.InitPairsCache(pairs);
            }
        }
    }
}