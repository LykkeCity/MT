using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Core;

namespace MarginTrading.Frontend.Services
{
    public interface IWatchListService
    {
        Task<List<MarginTradingWatchList>> GetAllAsync(string clientId);
        Task<IMarginTradingWatchList> GetAsync(string clientId, string id);
        Task<WatchListResult<IMarginTradingWatchList>> AddAsync(string id, string clientId, string name, List<string> assetIds);

        Task<WatchListResult<bool>> DeleteAsync(string clientId, string id);
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
