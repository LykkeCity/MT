using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.AssetPairSettings;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IAssetPairsEditingApi
    {
        /// <summary>
        /// Insert new pair
        /// </summary>
        [Post("/api/AssetPairs/{assetPairId}")]
        Task<AssetPairContract> Insert(string assetPairId, [Body] AssetPairInputContract settings);

        /// <summary>
        /// Update existing pair
        /// </summary>
        [Put("/api/AssetPairs/{assetPairId}")]
        Task<AssetPairContract> Update(string assetPairId, [Body] AssetPairInputContract settings);

        /// <summary>
        /// Delete existing pair
        /// </summary>
        [Delete("/api/AssetPairs/{assetPairId}")]
        Task<AssetPairContract> Delete(string assetPairId);
    }
}