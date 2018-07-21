using System.Threading.Tasks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
{
    public interface ITradingEngine
    {
        Task<Order> PlaceOrderAsync(Order order);

        Task<Order> ClosePositionAsync(string orderId, OriginatorType originator, string additionalInfo,
            string correlationId, string comment = null);
        Order CancelPendingOrder(string orderId, OriginatorType originator, string additionalInfo,
            string comment = null);
        void ChangeOrderLimits(string orderId, decimal price, OriginatorType originator, string additionalInfo);
        bool PingLock();
    }
}
