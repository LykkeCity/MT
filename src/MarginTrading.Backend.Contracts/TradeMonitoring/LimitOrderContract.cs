// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
    public class LimitOrderContract : BaseOrderContract
    {
        public string MarketMakerId { get; set; }
        public decimal Price { get; set; }
    }
}
