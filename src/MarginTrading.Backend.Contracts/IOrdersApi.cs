using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
        /// Place new order
        /// </summary>
        [Post("/api/orders")]
        Task PlaceAsync([Body] OrderPlaceRequest request);
        
        /// <summary>
        /// Change existing order
        /// </summary>
        [Put("/api/orders/{orderId}")]
        Task ChangeAsync(string orderId, [Body] OrderChangeRequest request);

        /// <summary>
        /// Close existing order 
        /// </summary>
        [Delete("/api/orders/{orderId}")]
        Task CancelAsync(string orderId, [Body] OrderCancelRequest request);
        
        /// <summary>
        /// Get order by id 
        /// </summary>
        [Get("/api/orders/{orderId}"), ItemCanBeNull]
        Task<OrderContract> GetAsync(string orderId);

        /// <summary>
        /// Get orders by parent order id
        /// </summary>
        [Get("/api/orders/by-parent-order/{parentOrderId}")]
        Task<List<OrderContract>> ListByParentOrderAsync(string parentOrderId);

        /// <summary>
        /// Get orders by parent position id
        /// </summary>
        [Get("/api/orders/by-parent-position/{parentPositionId}")]
        Task<List<OrderContract>> ListByParentPositionAsync(string parentPositionId);

        /// <summary>
        /// Get open orders with optional filtering
        /// </summary>
        [Get("/api/orders")]
        Task<List<OrderContract>> ListOpenAsync([Query, CanBeNull] string accountId = null,
            [Query, CanBeNull] string assetPairId = null);

        // todo: add filter by positionId?
        //todo: move to history
        /// <summary>
        /// Get executed orders with optional filtering
        /// </summary>
        [Get("/api/orders/executed")]
        Task<List<OrderContract>> ListExecutedAsync([Query, CanBeNull] string accountId = null,
            [Query, CanBeNull] string assetPairId = null);
    }
}