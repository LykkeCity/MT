using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.Events
{
    public class OrderChangedEventArgs: OrderUpdateBaseEventArgs
    {
        public OrderChangedEventArgs(Order order):base(order)
        {
        }

        public override OrderUpdateType UpdateType => OrderUpdateType.Change;
    }
}