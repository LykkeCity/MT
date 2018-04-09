using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.AssetPairSettings;
using MarginTrading.Backend.Contracts.Client;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IAssetPairsReadingApi
    {
        /// <summary>
        /// Get all pairs.
        /// Cached on client for 3 minutes 
        /// </summary>
        [Get("/api/AssetPairs/"), ClientCaching(Minutes = 3)]
        Task<List<AssetPairContract>> List([Query, CanBeNull] string legalEntity = null,
            [Query] MatchingEngineModeContract? matchingEngineMode = null);

        /// <summary>
        /// Get pair by id
        /// </summary>
        [Get("/api/AssetPairs/{assetPairId}")]
        Task<AssetPairContract> Get(string assetPairId);
    }
}