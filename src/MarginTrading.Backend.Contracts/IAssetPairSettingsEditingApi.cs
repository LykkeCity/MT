using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.AssetPairSettings;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IAssetPairSettingsEditingApi
    {
        /// <summary>
        /// Insert new settings for a pair
        /// </summary>
        [Post("/api/AssetPairSettings/{assetPairId}")]
        Task<AssetPairSettingsContract> Insert(string assetPairId, [Body] AssetPairSettingsInputContract settings);

        /// <summary>
        /// Update existing settings for a pair
        /// </summary>
        [Put("/api/AssetPairSettings/{assetPairId}")]
        Task<AssetPairSettingsContract> Update(string assetPairId, [Body] AssetPairSettingsInputContract settings);

        /// <summary>
        /// Delete existing settings for a pair
        /// </summary>
        [Delete("/api/AssetPairSettings/{assetPairId}")]
        Task<AssetPairSettingsContract> Delete(string assetPairId);
    }
}