using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Services.Events
{
    public class OrderLimitsChangedEventArgs: OrderUpdateBaseEventArgs
    {
        public OrderLimitsChangedEventArgs(Position order):base(order)
        {
        }

        public override OrderUpdateType UpdateType => OrderUpdateType.ChangeOrderLimits;
    }
}