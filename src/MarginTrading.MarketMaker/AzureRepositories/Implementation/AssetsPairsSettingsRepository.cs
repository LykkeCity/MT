using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.MarketMaker.AzureRepositories.Entities;

namespace MarginTrading.MarketMaker.AzureRepositories.Implementation
{
    internal class AssetsPairsSettingsRepository : IAssetsPairsSettingsRepository
    {
        private readonly INoSQLTableStorage<AssetPairSettingsEntity> _tableStorage;

        public AssetsPairsSettingsRepository(IAzureRepoFactory repoFactory)
            => _tableStorage = repoFactory.CreateStorage<AssetPairSettingsEntity>("MarketMakerAssetPairsSettings");

        public Task SetAsync(AssetPairSettingsEntity entity) => _tableStorage.InsertOrReplaceAsync(entity);

        public Task<AssetPairSettingsEntity> GetAsync(string partitionKey, string rowKey) => _tableStorage.GetDataAsync(partitionKey, rowKey);
    }
}