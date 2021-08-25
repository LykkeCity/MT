// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.Snow.Prices;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public interface IFakeSnapshotService
    {
        Task<string> AddOrUpdateFakeTradingDataSnapshot(DateTime tradingDay,
            string correlationId,
            List<OrderContract> orders,
            List<OpenPositionContract> positions,
            List<AccountStatContract> accounts,
            Dictionary<string, BestPriceContract> bestFxPrices,
            Dictionary<string, BestPriceContract> bestTradingPrices);

        Task DeleteFakeTradingSnapshot(string correlationId);
    }
}