using AzureStorage;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.MarketMaker.AzureRepositories
{
    internal interface IAzureRepoFactory
    {
        INoSQLTableStorage<TEntity> CreateStorage<TEntity>(string tableName) where TEntity : class, ITableEntity, new();
    }
}