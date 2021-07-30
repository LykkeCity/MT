// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Testing;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    public interface ITestingApi
    {
        /// <summary>
        /// Save snapshot of provided orders, positions, account stats, best fx prices, best trading prices.
        /// FOR TEST PURPOSES ONLY.
        /// </summary>
        /// <returns>Snapshot statistics.</returns>
        [Post("/api/testing/snapshot")]
        Task<string> AddOrUpdateTradingDataSnapshot(
            [Body] AddOrUpdateFakeSnapshotRequest request,
            [Query] string protectionKey);

        /// <summary>
        /// Deletes trading data snapshot
        /// FOR TEST PURPOSES ONLY.
        /// </summary>
        [Delete("/api/testing/snapshot")]
        Task<string> DeleteTradingDataSnapshot([Query] string correlationId, [Query] string protectionKey);
    }
}