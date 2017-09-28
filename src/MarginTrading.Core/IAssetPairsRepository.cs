using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IAssetPair
    {
        string Id { get; }
        string Name { get; }
        string BaseAssetId { get; }
        string QuoteAssetId { get; }
        int Accuracy { get; }
    }

    public class AssetPair : IAssetPair
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BaseAssetId { get; set; }
        public string QuoteAssetId { get; set; }
        public int Accuracy { get; set; }

        public static AssetPair Create(IAssetPair src)
        {
            return new AssetPair
            {
                Id = src.Id,
                Name = src.Name,
                BaseAssetId = src.BaseAssetId,
                QuoteAssetId = src.QuoteAssetId,
                Accuracy = src.Accuracy
            };
        }
    }

    public interface IAssetPairsRepository
    {
        Task<IEnumerable<IAssetPair>> GetAllAsync();
        Task<IEnumerable<IAssetPair>> GetAllAsync(List<string> instruments);
        Task AddAsync(IAssetPair assetPair);
        Task<IAssetPair> GetAssetAsync(string coreSymbol);
    }
}
