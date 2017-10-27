using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.MarketMaker.AzureRepositories
{
    internal interface IAbstractRepository<TEntity> where TEntity : ITableEntity, new()
    {
        Task InsertOrReplaceAsync(TEntity entity);
        [ItemCanBeNull]
        Task<TEntity> GetAsync(string partitionKey, string rowKey);
        Task<IList<TEntity>> GetAllAsync();
        Task DeleteIfExistAsync(string partitionKey, string rowKey);
    }
}