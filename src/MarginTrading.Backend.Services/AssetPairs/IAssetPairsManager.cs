using System.Threading.Tasks;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.AssetPairs
{
    public interface IAssetPairsManager
    {
        Task<IAssetPair> UpdateAssetPairSettings(IAssetPair assetPairSettings);
        Task<IAssetPair> InsertAssetPairSettings(IAssetPair assetPairSettings);
        Task<IAssetPair> DeleteAssetPairSettings(string assetPairId);
    }
}