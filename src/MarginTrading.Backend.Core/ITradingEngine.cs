using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
    public interface ITradingEngine
    {
        Task<Order> PlaceOrderAsync(Order order);
        Task<Order> CloseActiveOrderAsync(string orderId, OrderCloseReason reason, string comment = null);
        Task<Order> CancelPendingOrder(string orderId, OrderCloseReason reason, string comment = null);
        Task ChangeOrderLimits(string orderId, decimal? stopLoss, decimal? takeProfit, decimal? expectedOpenPrice);
        bool PingLock();
    }
}
