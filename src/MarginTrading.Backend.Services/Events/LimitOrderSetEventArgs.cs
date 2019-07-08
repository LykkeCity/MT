// Copyright (c) 2019 Lykke Corp.

using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Events
{
    public class LimitOrderSetEventArgs
    {
        public IEnumerable<LimitOrder> PlacedLimitOrders { get; set; } = new List<LimitOrder>();
        public IEnumerable<LimitOrder> DeletedLimitOrders { get; set; } = new List<LimitOrder>();

        public bool HasEvents()
        {
            return PlacedLimitOrders.Count() > 0 || DeletedLimitOrders.Count() > 0;
        }
    }
}
