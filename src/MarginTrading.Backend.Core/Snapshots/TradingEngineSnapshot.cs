// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Snapshots
{
    public class TradingEngineSnapshot
    {
        public DateTime TradingDay { get; }

        public string CorrelationId { get; }

        public DateTime Timestamp { get; }

        public string OrdersJson { get; private set; }

        public string PositionsJson { get; private set; }

        public string AccountsJson { get; }

        public string BestFxPricesJson { get; }

        public string BestTradingPricesJson { get; }

        public SnapshotStatus Status { get; }

        /// <summary>
        /// for dapper
        /// </summary>
        public TradingEngineSnapshot()
        {
        }

        public TradingEngineSnapshot(DateTime tradingDay,
            string correlationId,
            DateTime timestamp,
            string ordersJson,
            string positionsJson,
            string accountsJson,
            string bestFxPricesJson,
            string bestTradingPricesJson,
            SnapshotStatus status)
        {
            TradingDay = tradingDay;
            CorrelationId = correlationId;
            Timestamp = timestamp;
            OrdersJson = ordersJson;
            PositionsJson = positionsJson;
            AccountsJson = accountsJson;
            BestFxPricesJson = bestFxPricesJson;
            BestTradingPricesJson = bestTradingPricesJson;
            Status = status;
        }

        public void UpdatePositionsFromJson(string json)
        {
            PositionsJson = json;
        }

        public void UpdateOrdersFromJson(string json)
        {
            OrdersJson = json;
        }
    }
}