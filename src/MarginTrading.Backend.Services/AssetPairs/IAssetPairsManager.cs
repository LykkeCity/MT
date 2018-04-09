using System.Threading.Tasks;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.AssetPairs
{
    public interface IAssetPairsManager
    {
        Task<IAssetPair> UpdateAssetPair(IAssetPair assetPair);
        Task<IAssetPair> InsertAssetPair(IAssetPair assetPair);
        Task<IAssetPair> DeleteAssetPair(string assetPairId);
    }
}