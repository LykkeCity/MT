// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.AzureRepositories
{
    public class TradingEngineSnapshotsRepository : ITradingEngineSnapshotsRepository
    {
        public Task<TradingEngineSnapshot> GetLastAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<TradingEngineSnapshot> GetLastDraftAsync(DateTime? tradingDay)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(TradingEngineSnapshot tradingEngineSnapshot)
        {
            throw new NotImplementedException();
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