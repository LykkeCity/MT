// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.Events
{
    public class OrderExecutionStartedEventArgs: OrderUpdateBaseEventArgs
    {
        public OrderExecutionStartedEventArgs([NotNull] Order order) : base(order)
        {
        }

        public override OrderUpdateType UpdateType => OrderUpdateType.ExecutionStarted;
    }
}