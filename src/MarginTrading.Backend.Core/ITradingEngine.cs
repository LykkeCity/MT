using System.Threading.Tasks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
{
    public interface ITradingEngine
    {
        Task<Order> PlaceOrderAsync(Order order);
        Task<Position> CloseActiveOrderAsync(string orderId, OrderCloseReason reason, string comment = null);
        Position CancelPendingOrder(string orderId, OrderCloseReason reason, string comment = null);
        void ChangeOrderLimits(string orderId, decimal? stopLoss, decimal? takeProfit, decimal? expectedOpenPrice);
        bool PingLock();
    }
}
