// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.SqlRepositories.Entities
{
    public class TradingEngineSnapshotEntity
    {
        // Constructor is required for entity materialization by Dapper
        [UsedImplicitly]
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
            Status = tradingEngineSnapshot.Status.ToString();
        }

        public DateTime TradingDay { get; set; }

        public string CorrelationId { get; set; }

        public DateTime Timestamp { get; set; }

        public string Orders { get; set; }

        public string Positions { get; set; }

        public string AccountStats { get; set; }

        public string BestFxPrices { get; set; }

        public string BestPrices { get; set; }
        
        public string Status { get; set; }

        internal TradingEngineSnapshot ToDomain()
        {
            if (!Enum.TryParse<SnapshotStatus>(Status, out var status))
            {
                throw new ArgumentOutOfRangeException(nameof(Status),
                    $"Unrecognized value of snapshot status ({Status})");
            }

            return new TradingEngineSnapshot(
                TradingDay,
                CorrelationId,
                Timestamp,
                Orders,
                Positions,
                AccountStats,
                BestFxPrices,
                BestPrices,
                status);
        }
    }
}