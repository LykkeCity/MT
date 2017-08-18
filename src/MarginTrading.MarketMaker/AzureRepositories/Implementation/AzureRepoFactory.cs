using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.MarketMaker.Settings;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.MarketMaker.AzureRepositories.Implementation
{
    internal class AzureRepoFactory : IAzureRepoFactory
    {
        private readonly AppSettings _appSettings;
        private readonly ILog _log;

        public AzureRepoFactory(AppSettings appSettings, ILog log)
        {
            _appSettings = appSettings;
            _log = log;
        }

        public INoSQLTableStorage<TEntity> CreateStorage<TEntity>(string tableName) where TEntity : class, ITableEntity, new()
            => new AzureTableStorage<TEntity>(_appSettings.Db.ConnectionString, tableName, _log);
    }
}
