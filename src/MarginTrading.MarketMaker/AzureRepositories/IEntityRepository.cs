using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.MarketMaker.AzureRepositories
{
    internal interface IEntityRepository<TEntity>
    {
        Task SetAsync(TEntity entity);

        [ItemCanBeNull]
        Task<TEntity> GetAsync(string partitionKey, string rowKey);
    }
}
