// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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

        public string ActivitiesMetadata { get; protected set; }
    }
}