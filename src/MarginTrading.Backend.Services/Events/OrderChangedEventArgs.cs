using MarginTrading.Backend.Contracts.Activities;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.Events
{
    public class OrderChangedEventArgs: OrderUpdateBaseEventArgs
    {
        public OrderChangedEventArgs(Order order,  OrderUpdateMetadata metadata)
            :base(order)
        {
        }

        public override OrderUpdateType UpdateType => OrderUpdateType.Change;
    }
}