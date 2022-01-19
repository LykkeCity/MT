// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Common;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Contracts.TradingSchedule;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.Extensions
{
    public static class TradingEngineSnapshotExtensions
    {
        private static readonly string DraftStatusMessage = $"Snapshot is supposed to be in [{nameof(SnapshotStatus.Draft)}] status";

        public static List<OrderContract> GetOrders(this TradingEngineSnapshot snapshot)
        {
            return GetOrders<OrderContract>(snapshot);
        }

        public static List<Order> GetOrdersFromDraft(this TradingEngineSnapshot snapshot)
        {
            if (snapshot.Status != SnapshotStatus.Draft)
                throw new ArgumentOutOfRangeException(nameof(snapshot.Status), DraftStatusMessage);

            return GetOrders<Order>(snapshot);
        }

        public static List<OpenPositionContract> GetPositions(this TradingEngineSnapshot snapshot)
        {
            return GetPositions<OpenPositionContract>(snapshot);
        }

        public static List<Position> GetPositionsFromDraft(this TradingEngineSnapshot snapshot)
        {
            if (snapshot.Status != SnapshotStatus.Draft)
                throw new ArgumentOutOfRangeException(nameof(snapshot.Status), DraftStatusMessage);

            return GetPositions<Position>(snapshot);
        }
        
        public static List<AccountStatContract> GetAccounts(this TradingEngineSnapshot snapshot)
        {
            return GetAccounts<AccountStatContract>(snapshot);
        }

        public static List<MarginTradingAccount> GetAccountsFromDraft(this TradingEngineSnapshot snapshot)
        {
            if (snapshot.Status != SnapshotStatus.Draft)
                throw new ArgumentOutOfRangeException(nameof(snapshot.Status), DraftStatusMessage);
            
            return GetAccounts<MarginTradingAccount>(snapshot);
        }
        
        public static Dictionary<string, BestPriceContract> GetBestFxPrices(this TradingEngineSnapshot snapshot)
        {
            return snapshot.BestFxPricesJson == null
                ? new Dictionary<string, BestPriceContract>()
                : snapshot.BestFxPricesJson.DeserializeJson<Dictionary<string, BestPriceContract>>();
        }
        
        public static Dictionary<string, BestPriceContract> GetBestTradingPrices(this TradingEngineSnapshot snapshot)
        {
            return snapshot.BestTradingPricesJson == null
                ? new Dictionary<string, BestPriceContract>()
                : snapshot.BestTradingPricesJson.DeserializeJson<Dictionary<string, BestPriceContract>>();
        }


        public static bool IsPlatformClosureEvent(this MarketStateChangedEvent evt) =>
            evt.Id == LykkeConstants.PlatformMarketIdentifier && !evt.IsEnabled;
        
        private static List<T> GetOrders<T>(this TradingEngineSnapshot snapshot)
        {
            return snapshot.OrdersJson == null
                ? new List<T>()
                : snapshot.OrdersJson.DeserializeJson<List<T>>();
        }
        
        private static List<T> GetPositions<T>(this TradingEngineSnapshot snapshot)
        {
            return snapshot.PositionsJson == null
                ? new List<T>()
                : snapshot.PositionsJson.DeserializeJson<List<T>>();
        }
        
        private static List<T> GetAccounts<T>(this TradingEngineSnapshot snapshot)
        {
            return snapshot.AccountsJson == null
                ? new List<T>()
                : snapshot.AccountsJson.DeserializeJson<List<T>>();
        }
    }
}