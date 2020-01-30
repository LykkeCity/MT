// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.SqlRepositories.Entities
{
    public class TradingEngineSnapshotEntity
    {
        public TradingEngineSnapshotEntity()
        {
        }

        public TradingEngineSnapshotEntity(TradingEngineSnapshot tradingEngineSnapshot)
        {
            TradingDay = tradingEngineSnapshot.TradingDay;
            CorrelationId = tradingEngineSnapshot.CorrelationId;
            Timestamp = tradingEngineSnapshot.Timestamp;
            Orders = tradingEngineSnapshot.Orders;
            Positions = tradingEngineSnapshot.Positions;
            AccountStats = tradingEngineSnapshot.AccountStats;
            BestFxPrices = tradingEngineSnapshot.BestFxPrices;
            BestPrices = tradingEngineSnapshot.BestPrices;
        }

        public DateTime TradingDay { get; set; }

        public string CorrelationId { get; set; }

        public DateTime Timestamp { get; set; }

        public string Orders { get; set; }

        public string Positions { get; set; }

        public string AccountStats { get; set; }

        public string BestFxPrices { get; set; }

        public string BestPrices { get; set; }

        internal TradingEngineSnapshot ToDomain()
            => new TradingEngineSnapshot
            {
                TradingDay = TradingDay,
                CorrelationId = CorrelationId,
                Timestamp = Timestamp,
                Orders = Orders,
                Positions = Positions,
                AccountStats = AccountStats,
                BestFxPrices = BestFxPrices,
                BestPrices = BestPrices
            };
    }
}