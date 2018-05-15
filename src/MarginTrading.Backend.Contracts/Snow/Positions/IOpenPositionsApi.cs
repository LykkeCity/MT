using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Refit;

namespace MarginTrading.Backend.Contracts.Snow.Positions
{
    /// <summary>
    /// Gets info about positions
    /// </summary>
    [PublicAPI]
    public interface IOpenPositionsApi
    {
        /// <summary>
        /// Get a position by id
        /// </summary>
        [Get("/api/positions/open/{positionId}"), ItemCanBeNull]
        Task<OpenPositionContract> Get(string positionId);

        /// <summary>
        /// Get open positions with optional filtering
        /// </summary>
        [Get("/api/positions/open")]
        Task<List<OpenPositionContract>> List([Query, CanBeNull] string accountId = null,
            [Query, CanBeNull] string assetPairId = null);

        // todo: move to history?
//        /// <summary>
//        /// Get closed positions
//        /// </summary>
//        [Get("/api/positions/closed")]
//        Task<List<ClosedPositionContract>> ListClosed([Query, CanBeNull] string accountId,
//            [Query, CanBeNull] string instrument);
    }
}