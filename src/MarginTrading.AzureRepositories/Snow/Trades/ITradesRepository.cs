using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.AzureRepositories.Snow.Trades
{
    public interface ITradesRepository
    {
        [ItemCanBeNull]
        Task<TradeEntity> GetAsync(string id);
        Task UpsertAsync(TradeEntity entity);
    }
}