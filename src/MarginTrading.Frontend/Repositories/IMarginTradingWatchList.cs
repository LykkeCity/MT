using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Frontend.Repositories
{
    public interface IMarginTradingWatchList
    {
        string Id { get; }
        string ClientId { get; }
        string Name { get; }
        bool ReadOnly { get; set; }
        int Order { get; }
        List<string> AssetIds { get; }
    }

    public class MarginTradingWatchList : IMarginTradingWatchList
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string Name { get; set; }
        public bool ReadOnly { get; set; }
        public int Order { get; set; }
        public List<string> AssetIds { get; set; }

        public static MarginTradingWatchList Create(IMarginTradingWatchList src)
        {
            return new MarginTradingWatchList
            {
                Id = src.Id,
                ClientId = src.ClientId,
                AssetIds = src.AssetIds,
                Order = src.Order,
                Name = src.Name,
                ReadOnly = src.ReadOnly
            };
        }
    }

    public interface IMarginTradingWatchListRepository
    {
        Task<IMarginTradingWatchList> AddAsync(IMarginTradingWatchList watchList);
        Task ChangeAllAsync(IEnumerable<IMarginTradingWatchList> watchLists);
        Task<IEnumerable<IMarginTradingWatchList>> GetAllAsync(string accountId);
        Task<IMarginTradingWatchList> GetAsync(string accountId, string id);
        Task DeleteAsync(string accountId, string id);
    }
}
