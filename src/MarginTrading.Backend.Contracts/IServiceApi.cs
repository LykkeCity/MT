using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// Api to manage service functions
    /// </summary>
    [PublicAPI]
    public interface IServiceApi
    {
        /// <summary>
        /// Save snapshot of orders, positions, account stats, best fx prices, best trading prices for current moment.
        /// Throws an error in case if trading is not stopped.
        /// </summary>
        /// <returns>Snapshot statistics.</returns>
        [Post("/api/service/make-trading-data-snapshot")]
        Task<string> MakeTradingDataSnapshot([Query] DateTime tradingDay, 
            [Query, CanBeNull] string correlationId = null);
        
        /// <summary>
        /// Get current state of overnight margin parameter.
        /// </summary>
        [Get("/api/service/current-overnight-margin-parameter")]
        Task<bool> GetOvernightMarginParameterCurrentState();

        /// <summary>
        /// Get current margin parameter values for instruments (all / filtered by IDs).
        /// </summary>
        /// <returns>
        /// Dictionary with key = asset pair ID and value = (Dictionary with key = trading condition ID and value = multiplier)
        /// </returns>
        [Get("/api/service/overnight-margin-parameter")]
        Task<Dictionary<string, Dictionary<string, decimal>>> GetOvernightMarginParameterValues(
            [Query, CanBeNull] string[] instruments = null);
    }
}