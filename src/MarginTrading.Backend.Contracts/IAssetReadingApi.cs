using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.AssetPairSettings;
using MarginTrading.Backend.Contracts.AssetSettings;
using MarginTrading.Backend.Contracts.Client;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IAssetReadingApi
    {
        /// <summary>
        /// Get all Assets.
        /// Cached on client for 3 minutes 
        /// </summary>
        [Get("/api/Asset/"), ClientCaching(Minutes = 3)]
        Task<List<AssetContract>> List();

        /// <summary>
        /// Get pair by id
        /// </summary>
        [Get("/api/Asset/{assetId}")]
        Task<AssetContract> Get(string assetId);
    }
}