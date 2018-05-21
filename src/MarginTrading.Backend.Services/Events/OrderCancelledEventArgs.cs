using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;

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