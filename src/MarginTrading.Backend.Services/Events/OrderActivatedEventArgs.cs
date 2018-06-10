using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Services.Events
{
    public class OrderActivatedEventArgs: OrderUpdateBaseEventArgs
    {
        public OrderActivatedEventArgs([NotNull] Position order) : base(order)
        {
        }

        public override OrderUpdateType UpdateType => OrderUpdateType.Activate;
    }
}