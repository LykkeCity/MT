using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Positions;
using Refit;
using PositionDirectionContract = MarginTrading.Backend.Contracts.Positions.PositionDirectionContract;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// API for performing operations with positions and getting current state
    /// </summary>
    [PublicAPI]
    public interface IPositionsApi
    {
        /// <summary>
        /// Close a position 
        /// </summary>
        [Delete("/api/positions/{positionId}")]
        Task CloseAsync(string positionId, PositionCloseRequest request);

        /// <summary>
        /// Close close all positions by instrument and optionally direction 
        /// </summary>
        [Delete("/api/positions/instrument-group/{instrumentId}")]
        Task CloseGroupAsync(string instrument, PositionCloseRequest request,
            PositionDirectionContract? direction = null);
        
        /// <summary>
        /// Get a position by id
        /// </summary>
        [Get("/api/positions/{positionId}"), ItemCanBeNull]
        Task<OpenPositionContract> GetAsync(string positionId);

        /// <summary>
        /// Get positions with optional filtering
        /// </summary>
        [Get("/api/positions")]
        Task<List<OpenPositionContract>> ListAsync([Query, CanBeNull] string accountId = null,
            [Query, CanBeNull] string assetPairId = null);

        // todo: move to history
//        /// <summary>
//        /// Get closed positions
//        /// </summary>
//        [Get("/api/positions/closed")]
//        Task<List<ClosedPositionContract>> ListClosed([Query, CanBeNull] string accountId,
//            [Query, CanBeNull] string instrument);
    }
}