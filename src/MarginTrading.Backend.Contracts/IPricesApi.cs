using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Contracts.Snow.Prices;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>                                                                                       
    /// Provides data about prices
    /// </summary>
    [PublicAPI]
    public interface IPricesApi
    {
        /// <summary>
        /// Get current best prices
        /// </summary>
        /// <remarks>
        /// Post because the query string will be too long otherwise
        /// </remarks>
        [Post("/api/prices/best")]
        Task<Dictionary<string, BestPriceContract>> GetBestAsync([Body][NotNull] InitPricesBackendRequest request);
    }
}