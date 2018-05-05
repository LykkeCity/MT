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
    public interface IPositionsApi
    {
        // todo: Querying a closed position should be moved to the history service?
        // todo But in that case if we query this method with an already closed position id - it will not find the order.
        /// <summary>
        /// Get a position by id
        /// </summary>
        [Get("/api/positions/{positionId}"), ItemCanBeNull]
        Task<OpenPositionContract> Get(string positionId);

        /// <summary>
        /// Get open positions 
        /// </summary>
        [Get("/api/positions/open")]
        Task<List<OpenPositionContract>> ListOpen([Query, CanBeNull] string accountId,
            [Query, CanBeNull] string instrument);

        // todo: move to history?
//        /// <summary>
//        /// Get closed positions
//        /// </summary>
//        [Get("/api/positions/closed")]
//        Task<List<ClosedPositionContract>> ListClosed([Query, CanBeNull] string accountId,
//            [Query, CanBeNull] string instrument);
    }
}