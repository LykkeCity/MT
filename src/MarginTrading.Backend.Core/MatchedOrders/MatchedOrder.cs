// Copyright (c) 2019 Lykke Corp.

using System;

namespace MarginTrading.Backend.Core.MatchedOrders
{
    public class MatchedOrder
    {
        public string OrderId { get; set; }
        public string MarketMakerId { get; set; }
        public decimal LimitOrderLeftToMatch { get; set; }
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public DateTime MatchedDate { get; set; }
        public bool IsExternal { get; set; }
    }
}