// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Contract.BackendContracts
{
    public class LimitOrderBackendContract
    {
        public string Id { get; set; }
        public string MarketMakerId { get; set; }
        public string Instrument { get; set; }
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public DateTime CreateDate { get; set; }
        public MatchedOrderBackendContract[] MatchedOrders { get; set; }
    }
}
