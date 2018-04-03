using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.AssetPairSettings;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IDictionariesReadingApi
    {
        /// <summary>
        /// Returns all asset pairs
        /// </summary>
        [Get("/api/dictionaries/assetPairs/")]
        Task<List<AssetPairContract>> AssetPairs();

        /// <summary>
        /// Returns all matching engines
        /// </summary>
        [Get("/api/dictionaries/matchingEngines/")]
        Task<List<string>> MatchingEngines();

        /// <summary>
        /// Returns all order types
        /// </summary>
        [Get("/api/dictionaries/orderTypes/")]
        Task<List<string>> OrderTypes();
    }
}
