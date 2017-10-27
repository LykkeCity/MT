using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.MarketMaker.AzureRepositories.Implementation
{
    internal abstract class AbstractRepository<TEntity> : IAbstractRepository<TEntity> where TEntity : ITableEntity, new()
    {
        protected readonly INoSQLTableStorage<TEntity> TableStorage;

        protected AbstractRepository(INoSQLTableStorage<TEntity> tableStorage)
        {
            TableStorage = tableStorage;
        }

        public Task InsertOrReplaceAsync(TEntity entity)
        {
            return TableStorage.InsertOrReplaceAsync(entity);
        }

        public Task<TEntity> GetAsync(string partitionKey, string rowKey)
        {
            return TableStorage.GetDataAsync(partitionKey, rowKey);
        }

        public Task<IList<TEntity>> GetAllAsync()
        {
            return TableStorage.GetDataAsync();
        }

        public Task DeleteIfExistAsync(string partitionKey, string rowKey)
        {
            return TableStorage.DeleteIfExistAsync(partitionKey, rowKey);
        }
    }
}
