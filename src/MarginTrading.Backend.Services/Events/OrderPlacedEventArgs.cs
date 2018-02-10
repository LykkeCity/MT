using System;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Events
{
    public class OrderPlacedEventArgs: OrderUpdateBaseEventArgs
    {
        public OrderPlacedEventArgs(Order order):base(order)
        {
        }

        public override OrderUpdateType UpdateType => OrderUpdateType.Place;
    }
}