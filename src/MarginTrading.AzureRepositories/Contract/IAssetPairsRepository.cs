using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;

namespace MarginTrading.AzureRepositories.Contract
{
    public interface IAssetPairsRepository
    {
        Task<IReadOnlyList<IAssetPair>> GetAsync();
        Task InsertAsync(IAssetPair settings);
        Task ReplaceAsync(IAssetPair settings);
        Task<IAssetPair> DeleteAsync(string assetPairId);
        Task<IAssetPair> GetAsync(string assetPairId);
    }
}