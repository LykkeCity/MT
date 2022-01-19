// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services
{
    /// <summary>
    /// Trading snapshot recalculation from draft to final using closure prices
    /// </summary>
    public interface IFinalSnapshotCalculator
    {
        /// <summary>
        /// Runs calculations in order to apply prices to draft snapshot
        /// </summary>
        /// <param name="fxRates">The list of fx closure rates</param>
        /// <param name="cfdQuotes">The list of cfd closure quotes</param>
        /// <param name="correlationId">The operation correlation identifier</param>
        /// <returns>New snapshot in Final status after all calculations have been applied to Draft</returns>
        Task<TradingEngineSnapshot> RunAsync(
            IEnumerable<ClosingFxRate> fxRates,
            IEnumerable<ClosingAssetPrice> cfdQuotes,
            string correlationId);
    }
}