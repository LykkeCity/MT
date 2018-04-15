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
        private static readonly object InitAssetPairsLock = new object();

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
            lock (InitAssetPairsLock)
            {
                var pairs = _assetPairsRepository.GetAsync().GetAwaiter().GetResult()
                    .ToDictionary(a => a.Id, s => s);
                _assetPairsCache.InitPairsCache(pairs);
            }
        }

        public async Task<IAssetPair> UpdateAssetPair(IAssetPair assetPair)
        {
            ValidatePair(assetPair);
            await _assetPairsRepository.ReplaceAsync(assetPair);
            InitAssetPairs();
            return _assetPairsCache.GetAssetPairByIdOrDefault(assetPair.Id)
                .RequiredNotNull("AssetPair " + assetPair.Id);
        }

        public async Task<IAssetPair> InsertAssetPair(IAssetPair assetPair)
        {
            ValidatePair(assetPair);
            await _assetPairsRepository.InsertAsync(assetPair);
            InitAssetPairs();
            return _assetPairsCache.GetAssetPairByIdOrDefault(assetPair.Id)
                .RequiredNotNull("AssetPair " + assetPair.Id);
        }

        public async Task<IAssetPair> DeleteAssetPair(string assetPairId)
        {
            var pair = await _assetPairsRepository.DeleteAsync(assetPairId);
            InitAssetPairs();
            return pair; 
        }

        private void ValidatePair(IAssetPair newValue)
        {
            if (newValue.BasePairId == null) 
                return;

            if (_assetPairsCache.GetAssetPairByIdOrDefault(newValue.BasePairId) == null)
                throw new InvalidOperationException($"BasePairId {newValue.BasePairId} does not exist");

            if (_assetPairsCache.GetAll().Any(s =>
                s.Id != newValue.Id &&
                s.BasePairId == newValue.BasePairId))
            {
                throw new InvalidOperationException($"BasePairId {newValue.BasePairId} cannot be added twice");
            }
        }
    }
}