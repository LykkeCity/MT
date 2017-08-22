using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.MarketMaker.Settings;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.MarketMaker.AzureRepositories.Implementation
{
    internal class AzureRepoFactory : IAzureRepoFactory
    {
        private readonly MarginTradingMarketMakerSettings _marginTradingMarketMakerSettings;
        private readonly ILog _log;

        public AzureRepoFactory(MarginTradingMarketMakerSettings marginTradingMarketMakerSettings, ILog log)
        {
            _marginTradingMarketMakerSettings = marginTradingMarketMakerSettings;
            _log = log;
        }

        public INoSQLTableStorage<TEntity> CreateStorage<TEntity>(string tableName)
            where TEntity : class, ITableEntity, new()
        {
            return new AzureTableStorage<TEntity>(_marginTradingMarketMakerSettings.Db.ConnectionString, tableName, _log);
        }
    }
}