// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.Orders;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// API for performing operations with orders and reading current state
    /// </summary>
    [PublicAPI]
    public interface IOrdersApi
    {
        /// <summary>
        /// Update related order
        /// </summary>
        [Patch("/api/orders/{positionId}")]
        Task UpdateRelatedOrderAsync(string positionId, [Body][NotNull] UpdateRelatedOrderRequest request);

        /// <summary>
        /// Update related order bulk
        /// </summary>
        [Patch("/api/orders/bulk")]
        Task<Dictionary<string, string>> UpdateRelatedOrderBulkAsync([Body][NotNull] UpdateRelatedOrderBulkRequest request);

        /// <summary>
        /// Place new order
        /// </summary>
        /// <param name="request">Order model</param>
        /// <returns>Order Id</returns>
        [Post("/api/orders")]
        Task<string> PlaceAsync([Body] [NotNull] OrderPlaceRequest request);

        /// <summary>
        /// Change existing order
        /// </summary>
        [Put("/api/orders/{orderId}")]
        Task ChangeAsync([NotNull] string orderId, [Body][NotNull] OrderChangeRequest request);

        /// <summary>
        /// Close existing order 
        /// </summary>
        [Delete("/api/orders/{orderId}")]
        Task CancelAsync([NotNull] string orderId, [Body] OrderCancelRequest request = null, [Query] string accountId = null);

        /// <summary>
        /// Close order bulk
        /// </summary>
        [Delete("/api/orders/bulk")]
        Task<Dictionary<string, string>> CancelBulkAsync([Body] OrderCancelBulkRequest request = null,
            string accountId = null);

        /// <summary>
        /// Close group of orders by accountId, assetPairId and direction.
        /// </summary>
        /// <param name="accountId">Mandatory</param>
        /// <param name="assetPairId">Optional</param>
        /// <param name="direction">Optional</param>
        /// <param name="includeLinkedToPositions">Optional, should orders, linked to positions, to be canceled</param>
        /// <param name="request">Optional</param>
        /// <returns>Dictionary of failed to close orderIds with exception message</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        [Delete("/api/orders/cancel-group")]
        Task<Dictionary<string, string>> CancelGroupAsync([Query] [NotNull] string accountId,
            [Query] [CanBeNull] string assetPairId = null,
            [Query] [CanBeNull] OrderDirectionContract? direction = null,
            [Query] bool includeLinkedToPositions = false,
            [Body] [CanBeNull] OrderCancelRequest request = null);

        /// <summary>
        /// Get order by id 
        /// </summary>
        [Get("/api/orders/{orderId}"), ItemCanBeNull]
        Task<OrderContract> GetAsync([NotNull] string orderId);

        /// <summary>
        /// Get open orders with optional filtering
        /// </summary>
        [Get("/api/orders")]
        Task<List<OrderContract>> ListAsync([Query, CanBeNull] string accountId = null,
            [Query, CanBeNull] string assetPairId = null, [Query, CanBeNull] string parentPositionId = null,
            [Query, CanBeNull] string parentOrderId = null);

        /// <summary>
        /// Get open orders with optional filtering and pagination. Sorted descending by default.
        /// </summary>
        [Get("/api/orders/by-pages")]
        Task<PaginatedResponseContract<OrderContract>> ListAsyncByPages([Query] [CanBeNull] string accountId = null,
            [Query] [CanBeNull] string assetPairId = null, [Query] [CanBeNull] string parentPositionId = null,
            [Query] [CanBeNull] string parentOrderId = null,
            [Query] [CanBeNull] int? skip = null, [Query] [CanBeNull] int? take = null,
            [Query] string order = "DESC");
    }
}