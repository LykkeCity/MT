using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingAsset
    {
        string Id { get; }
        string Name { get; }
        string BaseAssetId { get; }
        string QuoteAssetId { get; }
        int Accuracy { get; }
    }

    public class MarginTradingAsset : IMarginTradingAsset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BaseAssetId { get; set; }
        public string QuoteAssetId { get; set; }
        public int Accuracy { get; set; }

        public static MarginTradingAsset Create(IMarginTradingAsset src)
        {
            return new MarginTradingAsset
            {
                Id = src.Id,
                Name = src.Name,
                BaseAssetId = src.BaseAssetId,
                QuoteAssetId = src.QuoteAssetId,
                Accuracy = src.Accuracy
            };
        }
    }

    public interface IMarginTradingAssetsRepository
    {
        Task<IEnumerable<IMarginTradingAsset>> GetAllAsync();
        Task<IEnumerable<MarginTradingAsset>> GetAllAsync(List<string> instruments);

        Task AddAsync(IMarginTradingAsset asset);
        Task<IMarginTradingAsset> GetAssetAsync(string coreSymbol);
    }
}
