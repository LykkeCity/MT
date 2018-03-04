using JetBrains.Annotations;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Events
{
    public class OrderClosingEventArgs: OrderUpdateBaseEventArgs
    {
        public OrderClosingEventArgs([NotNull] Order order) : base(order)
        {
        }

        public override OrderUpdateType UpdateType => OrderUpdateType.Closing;
    }
}