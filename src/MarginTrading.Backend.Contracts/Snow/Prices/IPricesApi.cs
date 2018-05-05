using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Refit;

namespace MarginTrading.Backend.Contracts.Snow.Prices
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
        Task<List<BestPriceContract>> Best([Body] string[] assetPairsIds);
    }
}