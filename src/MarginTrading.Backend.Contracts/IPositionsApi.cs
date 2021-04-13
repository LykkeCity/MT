// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.Orders;
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
        Task<PositionCloseResponse> CloseAsync([NotNull] string positionId,
            [Body] PositionCloseRequest request = null,
            [Query] string accountId = null);

        /// <summary>
        /// Close group of opened positions by accountId, assetPairId and direction.
        /// AccountId must be passed. Method signature allow nulls for backward compatibility.
        /// </summary>
        [Delete("/api/positions/close-group")]
        Task<PositionsGroupCloseResponse> CloseGroupAsync([Query, CanBeNull] string assetPairId = null,
            [Query] string accountId = null,
            [Query, CanBeNull] PositionDirectionContract? direction = null,
            [Body, CanBeNull] PositionCloseRequest request = null);
        
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

        /// <summary>
        /// Get positions with optional filtering and pagination
        /// </summary>
        [Get("/api/positions/by-pages")]
        Task<PaginatedResponseContract<OpenPositionContract>> ListAsyncByPages(
            [Query] [CanBeNull] string accountId = null,
            [Query] [CanBeNull] string assetPairId = null,
            [Query] [CanBeNull] int? skip = null, [Query] [CanBeNull] int? take = null);
    }
}