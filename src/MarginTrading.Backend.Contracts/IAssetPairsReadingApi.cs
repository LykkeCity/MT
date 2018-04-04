using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.AssetPairSettings;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IAssetPairsReadingApi
    {
        /// <summary>
        /// Get all pairs
        /// </summary>
        [Get("/api/AssetPairs/")]
        Task<List<AssetPairContract>> List([Query, CanBeNull] string legalEntity = null,
            [Query] MatchingEngineModeContract? matchingEngineMode = null);

        /// <summary>
        /// Get pair by id
        /// </summary>
        [Get("/api/AssetPairs/{assetPairId}")]
        Task<AssetPairContract> Get(string assetPairId);
    }
}