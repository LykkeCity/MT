using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;

namespace MarginTrading.AzureRepositories.Contract
{
    public interface IAssetPairSettingsRepository
    {
        Task<IReadOnlyList<IAssetPairSettings>> GetAsync();
        Task InsertAsync(IAssetPairSettings settings);
        Task ReplaceAsync(IAssetPairSettings settings);
        Task<IAssetPairSettings> DeleteAsync(string assetPairId);
        Task<IAssetPairSettings> GetAsync(string assetPairId);
    }
}