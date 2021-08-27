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
            Orders = tradingEngineSnapshot.OrdersJson;
            Positions = tradingEngineSnapshot.PositionsJson;
            AccountStats = tradingEngineSnapshot.AccountsJson;
            BestFxPrices = tradingEngineSnapshot.BestFxPricesJson;
            BestPrices = tradingEngineSnapshot.BestTradingPricesJson;
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
                OrdersJson = Orders,
                PositionsJson = Positions,
                AccountsJson = AccountStats,
                BestFxPricesJson = BestFxPrices,
                BestTradingPricesJson = BestPrices
            };
    }
}