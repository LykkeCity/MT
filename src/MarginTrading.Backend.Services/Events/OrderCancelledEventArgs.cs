using Common;
using MarginTrading.Backend.Contracts.Activities;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.Events
{
    public class OrderCancelledEventArgs : OrderUpdateBaseEventArgs
    {
        public OrderCancelledEventArgs(Order order, OrderCancelledMetadata metadata)
            : base(order)
        {
            ActivitiesMetadata = metadata.ToJson();
        }

        public override OrderUpdateType UpdateType => OrderUpdateType.Cancel;
    }
}