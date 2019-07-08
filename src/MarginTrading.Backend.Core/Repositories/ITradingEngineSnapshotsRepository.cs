// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface ITradingEngineSnapshotsRepository
    {
        Task Add(DateTime tradingDay, string correlationId, DateTime timestamp, string orders, string positions,
            string accounts,
            string bestFxPrices, string bestTradingPrices);
    }
}