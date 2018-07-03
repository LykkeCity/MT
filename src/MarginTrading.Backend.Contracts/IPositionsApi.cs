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
        Task CloseAsync([NotNull] string positionId, [Body] PositionCloseRequest request = null);

        /// <summary>
        /// Close group of opened positions by instrument and direction (optional) 
        /// </summary>
        [Delete("/api/positions/instrument-group/{assetPairId}")]
        Task CloseGroupAsync([NotNull] string assetPairId,
            [Query, CanBeNull] PositionDirectionContract? direction = null,
            [Body] PositionCloseRequest request = null);

        /// <summary>
        /// Close group of opened positions by account and instrument (optional)
        /// </summary>
        [Delete("/api/positions/account-group/{accountId}")]
        Task CloseGroupAsync([NotNull] string accountId, 
            [Query, CanBeNull] string assetPairId = null,
            [Body] PositionCloseRequest request = null);
        
        /// <summary>
        /// Get a position by id
        /// </summary>
        [Get("/api/positions/{positionId}"), ItemCanBeNull]
        Task<OpenPositionContract> GetAsync([NotNull] string positionId);

        /// <summary>
        /// Get positions with optional filtering
        /// </summary>
        [Get("/api/positions")]
        Task<List<OpenPositionContract>> ListAsync([Query, CanBeNull] string accountId = null,
            [Query, CanBeNull] string assetPairId = null);
    }
}