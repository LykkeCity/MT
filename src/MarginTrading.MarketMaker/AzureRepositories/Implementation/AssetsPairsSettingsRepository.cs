using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.MarketMaker.AzureRepositories.Entities;
using MarginTrading.MarketMaker.Settings;

namespace MarginTrading.MarketMaker.AzureRepositories.Implementation
{
    internal class AssetsPairsSettingsRepository : IAssetsPairsSettingsRepository
    {
        private readonly INoSQLTableStorage<AssetPairSettingsEntity> _tableStorage;

        public AssetsPairsSettingsRepository(IReloadingManager<MarginTradingMarketMakerSettings> settings, ILog log)
        {
            _tableStorage = AzureTableStorage<AssetPairSettingsEntity>.Create(
                settings.Nested(s => s.Db.ConnectionString),
                "MarketMakerAssetPairsSettings", log);
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

        public Task DeleteAsync(string partitionKey, string rowKey)
        {
            return _tableStorage.DeleteIfExistAsync(partitionKey, rowKey);
        }
    }
}