using System;
using MarginTrading.Backend.Core;

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