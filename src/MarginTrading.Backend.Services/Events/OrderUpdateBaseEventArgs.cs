using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

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

        public string ActivitiesMetadata { get; private set; }

        public void SetActivitiesMetadata(string value)
        {
            ActivitiesMetadata = value;
        }
    }
}