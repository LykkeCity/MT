// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Snapshots
{
    public class TradingEngineSnapshot
    {
        public DateTime TradingDay { get; set; }

        public string CorrelationId { get; set; }

        public DateTime Timestamp { get; set; }

        public string Orders { get; set; }

        public string Positions { get; set; }

        public string AccountStats { get; set; }
        
        public string BestFxPrices { get; set; }

        public string BestPrices { get; set; }
    }
}