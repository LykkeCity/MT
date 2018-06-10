using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Services.Events
{
    public class OrderRejectedEventArgs: OrderUpdateBaseEventArgs
    {
        public OrderRejectedEventArgs([NotNull] Position order) : base(order)
        {
        }

        public override OrderUpdateType UpdateType => OrderUpdateType.Reject;
    }
}