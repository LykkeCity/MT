using System.Threading.Tasks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
{
    public interface ITradingEngine
    {
        Task<Order> PlaceOrderAsync(Order order);
        Task<Order> ClosePositionAsync(string orderId, PositionCloseReason reason, string comment = null);
        Order CancelPendingOrder(string orderId, PositionCloseReason reason, string comment = null);
        void ChangeOrderLimits(string orderId, decimal price);
        bool PingLock();
    }
}
