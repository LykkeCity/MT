using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Orders;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// API for performing operations with orders
    /// </summary>
    [PublicAPI]
    public interface IOrdersApi
    {
        [Post("/api/orders")]
        Task PlaceAsync([Body] OrderPlaceRequest request);
        
        [Put("/api/orders/{orderId}")]
        Task ChangeAsync(string orderId, [Body] OrderChangeRequest request);

        [Delete("/api/orders")]
        Task CancelAsync([Body] OrderCancelRequest request);

    }
}