using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Refit;

namespace MarginTrading.Backend.Contracts.Snow.Orders
{
    /// <summary>
    /// Provides data about orders
    /// </summary>
    [PublicAPI]
    public interface IOrdersApi
    {
        /// <summary>
        /// Get order by id 
        /// </summary>
        [Get("/api/orders/{orderId}"), ItemCanBeNull]
        Task<OrderContract> Get(string orderId);

        /// <summary>
        /// Get orders by parent order id
        /// </summary>
        [Get("/api/orders/by-parent-order/{parentOrderId}")]
        Task<List<OrderContract>> ListByParentOrder(string parentOrderId);

        /// <summary>
        /// Get orders by parent position id
        /// </summary>
        [Get("/api/orders/by-parent-position/{parentPositionId}")]
        Task<List<OrderContract>> ListByParentPosition(string parentPositionId);

        /// <summary>
        /// Get open orders with optional filtering
        /// </summary>
        [Get("/api/orders/open")]
        Task<List<OrderContract>> ListOpen([Query, CanBeNull] string accountId = null,
            [Query, CanBeNull] string assetPairId = null);

        // todo: add filter by positionId?
        //todo: move to history
        /// <summary>
        /// Get executed orders with optional filtering
        /// </summary>
        [Get("/api/orders/executed")]
        Task<List<OrderContract>> ListExecuted([Query, CanBeNull] string accountId = null,
            [Query, CanBeNull] string assetPairId = null);
    }
}