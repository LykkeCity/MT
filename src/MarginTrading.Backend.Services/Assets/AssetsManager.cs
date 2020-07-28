// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Services;
using MarginTrading.AssetService.Contracts;
using MarginTrading.AssetService.Contracts.Asset;

namespace MarginTrading.Backend.Services.Assets
{
    [UsedImplicitly]
    public class AssetsManager : IStartable, IAssetsManager
    {
        private readonly IAssetsApi _assets;
        private readonly AssetsCache _assetsCache;
        private readonly IConvertService _convertService;

        public AssetsManager(IAssetsApi assets,
            AssetsCache assetsCache,
            IConvertService convertService)
        {
            _assets = assets;
            _assetsCache = assetsCache;
            _convertService = convertService;
        }

        public void Start()
        {
            UpdateCacheAsync().Wait();
        }

        public async Task UpdateCacheAsync()
        {
            var assets = (await _assets.List())
                .ToDictionary(
                    a => a.Id,
                    a => (IAsset)_convertService.Convert<AssetContract, Asset>(a));
            
            _assetsCache.Init(assets);
        }
    }
}
