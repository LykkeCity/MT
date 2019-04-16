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
        /// Get current state of overnight margin parameter.
        /// </summary>
        [Get("/api/service/current-overnight-margin-parameter")]
        Task<bool> GetOvernightMarginParameterCurrentState();

        /// <summary>
        /// Get current margin parameter values for instruments (all / filtered by IDs).
        /// </summary>
        [Get("/api/service/overnight-margin-parameter")]
        Task<Dictionary<(string, string), decimal>> GetOvernightMarginParameterValues(
            [Query, CanBeNull] string[] instruments = null);
    }
}