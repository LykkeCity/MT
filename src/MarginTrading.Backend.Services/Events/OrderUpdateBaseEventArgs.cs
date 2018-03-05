using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Events
{
    public abstract class OrderUpdateBaseEventArgs
    {
        protected OrderUpdateBaseEventArgs([NotNull] Order order)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
        }

        public abstract OrderUpdateType UpdateType { get; }
        [NotNull] public Order Order { get; }
    }
}