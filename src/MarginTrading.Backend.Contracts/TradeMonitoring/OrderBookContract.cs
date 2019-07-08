// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
    public class OrderBookContract
    {
        public string Instrument { get; set; }
        public List<LimitOrderContract> Buy { get; set; }
        public List<LimitOrderContract> Sell { get; set; }
    }
}
