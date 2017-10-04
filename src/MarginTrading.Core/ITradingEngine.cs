using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface ITradingEngine
    {
        Task<Order> PlaceOrderAsync(Order order);
        Task<Order> CloseActiveOrderAsync(string orderId, OrderCloseReason reason);
        Order CancelPendingOrder(string orderId, OrderCloseReason reason);
        void ChangeOrderLimits(string orderId, decimal stopLoss, decimal takeProfit, decimal expectedOpenPrice = 0);
        bool PingLock();
    }
}
