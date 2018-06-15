using System.Threading.Tasks;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
    public interface ITradingEngine
    {
        Task<Order> PlaceOrderAsync(Order order);
        Task<Order> CloseActiveOrderAsync(string orderId, OrderCloseReason reason, string comment = null);
        Order CancelPendingOrder(string orderId, OrderCloseReason reason, string comment = null);
        void ChangeOrderLimits(string orderId, decimal? stopLoss, decimal? takeProfit, decimal? expectedOpenPrice);
        bool PingLock();
    }
}
