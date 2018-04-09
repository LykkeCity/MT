using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Routes;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IRoutesReadingApi
    {
        /// <summary>
        /// Returns all routes
        /// </summary>
        [Get("/api/routes/")]
        Task<List<MatchingEngineRouteContract>> List();

        /// <summary>
        /// Returns route by Id
        /// </summary>
        [Get("/api/routes/{id}")]
        Task<MatchingEngineRouteContract> GetById(string id);
    }
}
