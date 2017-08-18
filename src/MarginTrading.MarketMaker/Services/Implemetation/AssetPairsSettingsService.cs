using System.Threading.Tasks;
using MarginTrading.MarketMaker.AzureRepositories;
using MarginTrading.MarketMaker.AzureRepositories.Entities;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using Rocks.Caching;

namespace MarginTrading.MarketMaker.Services.Implemetation
{
    internal class AssetPairsSettingsService : CachedEntityAccessorService<AssetPairSettingsEntity>, IAssetPairsSettingsService
    {
        public AssetPairsSettingsService(ICacheProvider cacheProvider, IAssetsPairsSettingsRepository assetsPairsSettingsRepository)
            :base(cacheProvider, assetsPairsSettingsRepository) { }

        public Task SetAssetPairQuotesSource(string assetPairId, AssetPairQuotesSourceEnum assetPairQuotesSource)
            => UpdateByKey(GetKeys(assetPairId), e => e.PairQuotesSourceEnum = assetPairQuotesSource);

        public AssetPairQuotesSourceEnum? GetAssetPairQuotesSource(string assetPairId)
            => GetByKey(GetKeys(assetPairId))?.PairQuotesSourceEnum;

        private static (string PartitionKey, string RowKey) GetKeys(string assetPairId)
            => (AssetPairSettingsEntity.GeneratePartitionKey(), AssetPairSettingsEntity.GenerateRowKey(assetPairId));
    }

}