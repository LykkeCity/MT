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
        Task<string> MakeTradingDataSnapshot([Query, CanBeNull] string correlationId = null);
        
        /// <summary>
        /// Get current value of overnight margin parameter.
        /// </summary>
        [Get("/api/service/current-overnight-margin-parameter")]
        Task<decimal> GetCurrentOvernightMarginParameter();

        /// <summary>
        /// Get persisted value of overnight margin parameter.
        /// This value is applied at corresponding time, which depends on settings.
        /// </summary>
        [Get("/api/service/overnight-margin-parameter")]
        Task<decimal> GetOvernightMarginParameter();

        /// <summary>
        /// Set and persist new value of overnight margin parameter.
        /// </summary>
        [Put("/api/service/overnight-margin-parameter")]
        Task SetOvernightMarginParameter(decimal newValue, string correlationId = null);
    }
}