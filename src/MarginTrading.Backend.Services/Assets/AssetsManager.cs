using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Services.Assets
{
    public class AssetsManager : IStartable, IAssetManager
    {
        private readonly IAssetRepository _assetRepository;
        private readonly AssetsCache _assetsCache;

        public AssetsManager(IAssetRepository assetRepository,
            AssetsCache assetsCache)
        {
            _assetRepository = assetRepository;
            _assetsCache = assetsCache;
        }

        public void Start()
        {
            InitCache();
        }

        public void InitCache()
        {
            var assets = _assetRepository.GetAsync().GetAwaiter().GetResult()
                .ToDictionary(a => a.Id, s => s);
            _assetsCache.Init(assets);
        }

        public async Task<IAsset> UpdateAsset(IAsset asset)
        {
            await _assetRepository.ReplaceAsync(asset);
            InitCache();
            return _assetsCache.GetAssetById(asset.Id)
                .RequiredNotNull("Asset " + asset.Id);
        }

        public async Task<IAsset> InsertAsset(IAsset asset)
        {
            await _assetRepository.InsertAsync(asset);
            InitCache();
            return _assetsCache.GetAssetById(asset.Id)
                .RequiredNotNull("Asset " + asset.Id);
        }

        public async Task<IAsset> DeleteAsset(string assetId)
        {
            var asset = await _assetRepository.DeleteAsync(assetId);
            InitCache();
            return asset;
        }
    }
}
