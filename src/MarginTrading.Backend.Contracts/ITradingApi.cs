using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.Trading;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// API for performing trading operations
    /// </summary>
    [PublicAPI]
    public interface ITradingApi
    {
        /// <summary>
        /// Close position
        /// </summary>
        [Post("/api/mt/order.close")]
        Task<BackendResponse<bool>> CloseOrder(CloseOrderBackendRequest request);

        /// <summary>
        /// Cancel pending order
        /// </summary>
        [Post("/api/mt/order.cancel")]
        Task<BackendResponse<bool>> CancelOrder(CloseOrderBackendRequest request);
    }
}