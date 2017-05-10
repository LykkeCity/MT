using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IWatchListService
    {
        Task<List<MarginTradingWatchList>> GetAllAsync(string accountId);
        Task<IMarginTradingWatchList> GetAsync(string accountId, string id);
        Task<WatchListResult<IMarginTradingWatchList>> AddAsync(string id, string accountId, string name, List<string> assetIds);

        Task<WatchListResult<bool>> DeleteAsync(string accountId, string id);
    }

    public class WatchListResult<T>
    {
        public T Result { get; set; }
        public WatchListStatus Status { get; set; }
        public string Message { get; set; }
    }

    public enum WatchListStatus
    {
        Ok,
        NotFound,
        AssetNotFound,
        ReadOnly,
        AlreadyDefault
    }
}
