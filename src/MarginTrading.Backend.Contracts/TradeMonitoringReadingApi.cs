using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.TradeMonitoring;
using Refit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    interface TradeMonitoringReadingApi
    {
        /// <summary>
        /// Returns summary info by assets
        /// </summary>
        [Get("/api/trade/assets/summary/")]
        Task<List<AssetSummaryContract>> AssetSummaryList();

        /// <summary>
        /// Returns summary info by assets
        /// </summary>
        [Get("/api/trade/openPositions/byVolume/{volume}")]
        Task<List<AssetSummaryContract>> AssetSummaryList();
    }
}
