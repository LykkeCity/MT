// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Common;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Extensions
{
    public static class TradingEngineSnapshotExtensions
    {
        public static List<OrderContract> GetOrders(this TradingEngineSnapshot snapshot)
        {
            return snapshot.OrdersJson == null
                ? new List<OrderContract>()
                : snapshot.OrdersJson.DeserializeJson<List<OrderContract>>();
        }

        public static List<OpenPositionContract> GetPositions(this TradingEngineSnapshot snapshot)
        {
            return snapshot.PositionsJson == null
                ? new List<OpenPositionContract>()
                : snapshot.PositionsJson.DeserializeJson<List<OpenPositionContract>>();
        }
        
        public static List<AccountStatContract> GetAccounts(this TradingEngineSnapshot snapshot)
        {
            return snapshot.AccountsJson == null
                ? new List<AccountStatContract>()
                : snapshot.AccountsJson.DeserializeJson<List<AccountStatContract>>();
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
    }
}