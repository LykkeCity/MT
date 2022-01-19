// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Core.Services
{
    public interface ISnapshotService
    {
        /// <summary>
        /// Make final trading snapshot from current system state
        /// </summary>
        /// <param name="tradingDay"></param>
        /// <param name="correlationId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        Task<string> MakeTradingDataSnapshot(
            DateTime tradingDay, 
            string correlationId, 
            SnapshotStatus status = SnapshotStatus.Final);

        /// <summary>
        /// Make final trading snapshot from draft
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="cfdQuotes"></param>
        /// <param name="fxRates"></param>
        /// <returns></returns>
        Task MakeTradingDataSnapshotFromDraft( 
            string correlationId, 
            IEnumerable<ClosingAssetPrice> cfdQuotes,
            IEnumerable<ClosingFxRate> fxRates);
    }
}