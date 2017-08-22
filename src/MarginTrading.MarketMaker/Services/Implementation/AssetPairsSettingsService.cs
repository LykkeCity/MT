using System.Threading.Tasks;
using MarginTrading.MarketMaker.AzureRepositories;
using MarginTrading.MarketMaker.AzureRepositories.Entities;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using Rocks.Caching;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    internal class AssetPairsSettingsService : CachedEntityAccessorService<AssetPairSettingsEntity>,
        IAssetPairsSettingsService
    {
        public AssetPairsSettingsService(ICacheProvider cacheProvider,
            IAssetsPairsSettingsRepository assetsPairsSettingsRepository)
            : base(cacheProvider, assetsPairsSettingsRepository)
        {
        }

        public Task SetAssetPairQuotesSource(string assetPairId, AssetPairQuotesSourceEnum assetPairQuotesSource)
        {
            return UpdateByKey(GetKeys(assetPairId), e => e.QuotesSourceEnum = assetPairQuotesSource);
        }

        public AssetPairQuotesSourceEnum? GetAssetPairQuotesSource(string assetPairId)
        {
            return GetByKey(GetKeys(assetPairId))?.QuotesSourceEnum;
        }

        private static EntityKeys GetKeys(string assetPairId)
        {
            return new EntityKeys(AssetPairSettingsEntity.GeneratePartitionKey(), AssetPairSettingsEntity
                .GenerateRowKey(assetPairId));
        }
    }
}