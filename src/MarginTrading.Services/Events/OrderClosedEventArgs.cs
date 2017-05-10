using System;
using MarginTrading.Core;

namespace MarginTrading.Services.Events
{
    public class OrderClosedEventArgs
    {
        public OrderClosedEventArgs(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            Order = order;
        }

        public Order Order { get; }
    }
}