using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Services.Events
{
    public abstract class OrderUpdateBaseEventArgs
    {
        protected OrderUpdateBaseEventArgs([NotNull] Position order)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
        }

        public abstract OrderUpdateType UpdateType { get; }
        [NotNull] public Position Order { get; }
    }
}