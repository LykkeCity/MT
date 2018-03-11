using System.Threading.Tasks;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.AssetPairs
{
    public interface IAssetPairsManager
    {
        Task<IAssetPairSettings> UpdateAssetPairSettings(IAssetPairSettings assetPairSettings);
        Task<IAssetPairSettings> InsertAssetPairSettings(IAssetPairSettings assetPairSettings);
        Task<IAssetPairSettings> DeleteAssetPairSettings(string assetPairId);
    }
}