using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingAssetPair
    {
        string Id { get; }
        string Name { get; }
        string BaseAssetId { get; }
        string QuoteAssetId { get; }
        int Accuracy { get; }
    }

    public class MarginTradingAssetPair : IMarginTradingAssetPair
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BaseAssetId { get; set; }
        public string QuoteAssetId { get; set; }
        public int Accuracy { get; set; }

        public static MarginTradingAssetPair Create(IMarginTradingAssetPair src)
        {
            return new MarginTradingAssetPair
            {
                Id = src.Id,
                Name = src.Name,
                BaseAssetId = src.BaseAssetId,
                QuoteAssetId = src.QuoteAssetId,
                Accuracy = src.Accuracy
            };
        }
    }

    public interface IMarginTradingAssetPairsRepository
    {
        Task<IEnumerable<IMarginTradingAssetPair>> GetAllAsync();
        Task<IEnumerable<MarginTradingAssetPair>> GetAllAsync(List<string> instruments);
        Task AddAsync(IMarginTradingAssetPair assetPair);
        Task<IMarginTradingAssetPair> GetAssetAsync(string coreSymbol);
    }
}
