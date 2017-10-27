using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.AzureRepositories;
using MarginTrading.MarketMaker.AzureRepositories.Entities;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using MarginTrading.MarketMaker.Models.Api;
using Rocks.Caching;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    internal class AssetPairsSettingsService : CachedEntityAccessorService<AssetPairSettingsEntity>,
        IAssetPairsSettingsService
    {
        private readonly IAssetsPairsSettingsRepository _assetsPairsSettingsRepository;

        public AssetPairsSettingsService(ICacheProvider cache,
            IAssetsPairsSettingsRepository assetsPairsSettingsRepository)
            : base(cache, assetsPairsSettingsRepository)
        {
            _assetsPairsSettingsRepository = assetsPairsSettingsRepository;
        }

        public Task SetAssetPairQuotesSourceAsync(string assetPairId, AssetPairQuotesSourceTypeEnum assetPairQuotesSourceType)
        {
            return UpdateByKeyAsync(GetKeys(assetPairId), e => e.QuotesSourceType = assetPairQuotesSourceType);
        }

        public async Task<List<AssetPairSettings>> GetAllPairsSourcesAsync()
        {
            return (await _assetsPairsSettingsRepository.GetAllAsync())
                .Select(s => new AssetPairSettings
                {
                    AssetPairId = s.AssetPairId,
                    QuotesSourceType = s.QuotesSourceType,
                    Timestamp = s.Timestamp,
                }).ToList();
        }

        [CanBeNull]
        public AssetPairSettings Get(string assetPairId)
        {
            var entity = GetByKey(GetKeys(assetPairId));
            if (entity == null)
            {
                return null;
            }

            return new AssetPairSettings
            {
                AssetPairId = entity.AssetPairId,
                QuotesSourceType = entity.QuotesSourceType,
                Timestamp = entity.Timestamp,
            };
        }

        public async Task DeleteAsync(string assetPairId)
        {
            var keys = GetKeys(assetPairId);
            await _assetsPairsSettingsRepository.DeleteIfExistAsync(keys.PartitionKey, keys.RowKey);
            DeleteByKey(keys);
        }

        public AssetPairQuotesSourceTypeEnum? GetAssetPairQuotesSource(string assetPairId)
        {
            var entity = GetByKey(GetKeys(assetPairId));
            return entity?.QuotesSourceType;
        }

        private static EntityKeys GetKeys(string assetPairId)
        {
            return new EntityKeys(AssetPairSettingsEntity.GeneratePartitionKey(), AssetPairSettingsEntity
                .GenerateRowKey(assetPairId));
        }
    }
}