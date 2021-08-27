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

        Task Add(DateTime tradingDay, string correlationId, DateTime timestamp, string orders, string positions,
            string accounts,
            string bestFxPrices, string bestTradingPrices);

        Task<TradingEngineSnapshot> Get(string correlationId);

        Task Delete(string correlationId);
    }
}