using JetBrains.Annotations;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Events
{
    public class OrderActivatedEventArgs: OrderUpdateBaseEventArgs
    {
        public OrderActivatedEventArgs([NotNull] Order order) : base(order)
        {
        }

        public override OrderUpdateType UpdateType => OrderUpdateType.Activate;
    }
}