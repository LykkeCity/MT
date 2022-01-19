// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface ITradingEngineSnapshotsRepository
    {
        Task<TradingEngineSnapshot> GetLastAsync();
        
        Task<TradingEngineSnapshot> GetLastDraftAsync(DateTime? tradingDay);

        Task AddAsync(TradingEngineSnapshot tradingEngineSnapshot);

        Task<TradingEngineSnapshot> Get(string correlationId);

        Task Delete(string correlationId);

        Task<bool> DraftExistsAsync(DateTime tradingDay);
    }
}