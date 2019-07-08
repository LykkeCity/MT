// Copyright (c) 2019 Lykke Corp.

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.Events
{
    public class OrderExecutedEventArgs: OrderUpdateBaseEventArgs
    {
        public OrderExecutedEventArgs(Order order):base(order)
        {
        }

        public override OrderUpdateType UpdateType => OrderUpdateType.Executed;
    }
}