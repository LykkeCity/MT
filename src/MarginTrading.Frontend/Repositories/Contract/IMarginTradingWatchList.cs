using System.Collections.Generic;

namespace MarginTrading.Frontend.Repositories.Contract
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
}
