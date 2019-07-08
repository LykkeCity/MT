// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core.MatchedOrders;

namespace MarginTrading.Backend.Core.Orders
{
    public class BaseOrder : IBaseOrder
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Instrument { get; set; }
        public decimal Volume { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public MatchedOrderCollection MatchedOrders { get; set; } = new MatchedOrderCollection();
    }
}