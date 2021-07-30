// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core
{
    public class TradingEngineSnapshot
    {
        public DateTime TradingDay { get; set; }

        public string CorrelationId { get; set; }
        
        public string OrdersJson { get; set; }

        public string PositionsJson { get; set; }

        public string AccountsJson { get; set; }

        public string BestFxPricesJson { get; set; }

        public string BestTradingPricesJson { get; set; }
    }
}