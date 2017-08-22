using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.MarketMaker.AzureRepositories.Entities;
using MarginTrading.MarketMaker.Settings;

namespace MarginTrading.MarketMaker.AzureRepositories.Implementation
{
    internal class AssetsPairsSettingsRepository : IAssetsPairsSettingsRepository
    {
        private readonly INoSQLTableStorage<AssetPairSettingsEntity> _tableStorage;

        public AssetsPairsSettingsRepository(MarginTradingMarketMakerSettings settings, ILog log)
        {
            _tableStorage = new AzureTableStorage<AssetPairSettingsEntity>(settings.Db.ConnectionString, "MarketMakerAssetPairsSettings", log);
        }

        public Task SetAsync(AssetPairSettingsEntity entity)
        {
            return _tableStorage.InsertOrReplaceAsync(entity);
        }

        public Task<AssetPairSettingsEntity> GetAsync(string partitionKey, string rowKey)
        {
            return _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

        public Task<IList<AssetPairSettingsEntity>> GetAll()
        {
            return _tableStorage.GetDataAsync();
        }
    }
}