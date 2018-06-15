using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.Events
{
    public class OrderCancelledEventArgs:OrderUpdateBaseEventArgs
    {
        public OrderCancelledEventArgs(Order order): base(order)
        {
        }

        public override OrderUpdateType UpdateType => OrderUpdateType.Cancel;
    }
}