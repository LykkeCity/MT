using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Services.AssetPairs
{
    internal class AssetPairsManager : IStartable, IAssetPairsManager
    {
        private static readonly object InitAssetPairSettingsLock = new object();

        private readonly IAssetPairsInitializableCache _assetPairsCache;
        private readonly IAssetPairsRepository _assetPairsRepository;

        public AssetPairsManager(IAssetPairsInitializableCache assetPairsCache,
            IAssetPairsRepository assetPairsRepository)
        {
            _assetPairsCache = assetPairsCache;
            _assetPairsRepository = assetPairsRepository;
        }

        public void Start()
        {
            InitAssetPairs();
        }

        private void InitAssetPairs()
        {
            lock (InitAssetPairSettingsLock)
            {
                var settings = _assetPairsRepository.GetAsync().GetAwaiter().GetResult()
                    .ToDictionary(a => a.Id, s => s);
                _assetPairsCache.InitPairsCache(settings);
            }
        }

        public async Task<IAssetPair> UpdateAssetPairSettings(IAssetPair assetPairSettings)
        {
            ValidateSettings(assetPairSettings);
            await _assetPairsRepository.ReplaceAsync(assetPairSettings);
            InitAssetPairs();
            return _assetPairsCache.TryGetAssetPairById(assetPairSettings.Id)
                .RequiredNotNull("AssetPairSettings for " + assetPairSettings.Id);
        }

        public async Task<IAssetPair> InsertAssetPairSettings(IAssetPair assetPairSettings)
        {
            ValidateSettings(assetPairSettings);
            await _assetPairsRepository.InsertAsync(assetPairSettings);
            InitAssetPairs();
            return _assetPairsCache.TryGetAssetPairById(assetPairSettings.Id)
                .RequiredNotNull("AssetPairSettings for " + assetPairSettings.Id);
        }

        public async Task<IAssetPair> DeleteAssetPairSettings(string assetPairId)
        {
            var settings = await _assetPairsRepository.DeleteAsync(assetPairId);
            InitAssetPairs();
            return settings;
        }

        private void ValidateSettings(IAssetPair newValue)
        {
            if (newValue.BasePairId != null && _assetPairsCache.TryGetAssetPairById(newValue.BasePairId) == null)
            {
                throw new InvalidOperationException($"BasePairId {newValue.BasePairId} does not exist");
            }

            if (_assetPairsCache.GetAll().Any(s =>
                s.Id != newValue.Id &&
                s.BasePairId == newValue.BasePairId))
            {
                throw new InvalidOperationException($"BasePairId {newValue.BasePairId} cannot be added twice");
            }
        }
    }
}