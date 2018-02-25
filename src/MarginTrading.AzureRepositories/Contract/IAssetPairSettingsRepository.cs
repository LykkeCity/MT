using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;

namespace MarginTrading.AzureRepositories.Contract
{
    public interface IAssetPairSettingsRepository
    {
        Task<IReadOnlyList<IAssetPairSettings>> Get();
        Task Insert(IAssetPairSettings settings);
        Task Update(IAssetPairSettings settings);
        Task<IAssetPairSettings> Delete(string assetPairId);
        Task<IAssetPairSettings> Get(string assetPairId);
    }
}