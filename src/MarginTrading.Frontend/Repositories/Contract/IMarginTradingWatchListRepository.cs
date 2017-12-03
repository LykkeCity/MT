using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Frontend.Repositories.Contract
{
    public interface IMarginTradingWatchListRepository
    {
        Task<IMarginTradingWatchList> AddAsync(IMarginTradingWatchList watchList);
        Task ChangeAllAsync(IEnumerable<IMarginTradingWatchList> watchLists);
        Task<IEnumerable<IMarginTradingWatchList>> GetAllAsync(string accountId);
        Task<IMarginTradingWatchList> GetAsync(string accountId, string id);
        Task DeleteAsync(string accountId, string id);
    }
}