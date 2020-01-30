// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface ITradingEngineSnapshotsRepository
    {
        Task<TradingEngineSnapshot> GetLastAsync();

        Task AddAsync(TradingEngineSnapshot tradingEngineSnapshot);
    }
}