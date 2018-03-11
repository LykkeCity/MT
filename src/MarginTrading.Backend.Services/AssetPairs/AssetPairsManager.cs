using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Lykke.Service.Assets.Client;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Services.AssetPairs
{
    internal class AssetPairsManager : IStartable, IAssetPairsManager
    {
        private static readonly object InitAssetPairSettingsLock = new object();

        private readonly IAssetsService _assets;
        private readonly IAssetPairsInitializableCache _assetPairsCache;
        private readonly IAssetPairSettingsRepository _assetPairSettingsRepository;

        public AssetPairsManager(IAssetsService repository,
            IAssetPairsInitializableCache assetPairsCache,
            IAssetPairSettingsRepository assetPairSettingsRepository)
        {
            _assets = repository;
            _assetPairsCache = assetPairsCache;
            _assetPairSettingsRepository = assetPairSettingsRepository;
        }

        public void Start()
        {
            var instrumentsTask = UpdateInstrumentsCache();
            InitAssetPairSettings();
            instrumentsTask.Wait();
        }

        private async Task UpdateInstrumentsCache()
        {
            var instruments = (await _assets.AssetPairGetAllAsync())
                .ToDictionary(
                    a => a.Id,
                    a => (IAssetPair) new AssetPair(a.Id, a.Name, a.BaseAssetId, a.QuotingAssetId, a.Accuracy));

            _assetPairsCache.InitInstrumentsCache(instruments);
        }

        private void InitAssetPairSettings()
        {
            lock (InitAssetPairSettingsLock)
            {
                var settings = _assetPairSettingsRepository.GetAsync().GetAwaiter().GetResult()
                    .ToDictionary(a => a.AssetPairId, s => s);
                _assetPairsCache.InitAssetPairSettingsCache(settings);
            }
        }

        public async Task<IAssetPairSettings> UpdateAssetPairSettings(IAssetPairSettings assetPairSettings)
        {
            await _assetPairSettingsRepository.ReplaceAsync(assetPairSettings);
            InitAssetPairSettings();
            return _assetPairsCache.GetAssetPairSettings(assetPairSettings.AssetPairId)
                .RequiredNotNull("AssetPairSettings for " + assetPairSettings.AssetPairId);
        }

        public async Task<IAssetPairSettings> InsertAssetPairSettings(IAssetPairSettings assetPairSettings)
        {
            await _assetPairSettingsRepository.InsertAsync(assetPairSettings);
            InitAssetPairSettings();
            return _assetPairsCache.GetAssetPairSettings(assetPairSettings.AssetPairId)
                .RequiredNotNull("AssetPairSettings for " + assetPairSettings.AssetPairId);
        }

        public async Task<IAssetPairSettings> DeleteAssetPairSettings(string assetPairId)
        {
            var settings = await _assetPairSettingsRepository.DeleteAsync(assetPairId);
            InitAssetPairSettings();
            return settings;
        }
    }
}