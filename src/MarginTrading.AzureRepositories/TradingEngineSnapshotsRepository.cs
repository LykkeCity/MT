// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.AzureRepositories
{
    public class TradingEngineSnapshotsRepository : ITradingEngineSnapshotsRepository
    {
        public Task Add(DateTime tradingDay, string correlationId, DateTime timestamp, string orders, string positions,
            string accounts,
            string bestFxPrices, string bestTradingPrices)
        {
            throw new System.NotImplementedException();
        }

        public Task<TradingEngineSnapshot> Get(string correlationId)
        {
            throw new NotImplementedException();
        }

        public Task Delete(string correlationId)
        {
            throw new NotImplementedException();
        }
    }
}