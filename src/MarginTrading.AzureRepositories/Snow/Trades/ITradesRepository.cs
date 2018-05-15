using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.AzureRepositories.Snow.Trades
{
    public interface ITradesRepository
    {
        [ItemCanBeNull]
        Task<ITrade> GetAsync(string id);
        Task UpsertAsync(ITrade trade);
    }
}