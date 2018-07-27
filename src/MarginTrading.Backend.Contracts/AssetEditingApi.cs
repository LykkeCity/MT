using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.AssetPairSettings;
using MarginTrading.Backend.Contracts.AssetSettings;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IAssetEditingApi
    {
        /// <summary>
        /// Insert new asset
        /// </summary>
        [Post("/api/Asset/{assetId}")]
        Task<AssetContract> Insert(string assetId, [Body] AssetInputContract settings);

        /// <summary>
        /// Update existing asset
        /// </summary>
        [Put("/api/Asset/{assetId}")]
        Task<AssetContract> Update(string assetId, [Body] AssetInputContract settings);

        /// <summary>
        /// Delete existing asset
        /// </summary>
        [Delete("/api/Asset/{assetPairId}")]
        Task<AssetContract> Delete(string assetId);
    }
}
