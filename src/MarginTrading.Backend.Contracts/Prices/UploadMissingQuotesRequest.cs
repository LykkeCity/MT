// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Prices
{
    /// <summary>
    /// The request details for uploading missing quotes
    /// </summary>
    [PublicAPI]
    public class UploadMissingQuotesRequest
    {
        public string TradingDay { get; set; }
        
        public string CorrelationId { get; set; }
        
        public IEnumerable<ClosingAssetPrice> Cfd { get; set; }

        public IEnumerable<ClosingFxRate> Forex { get; set; }
    }
}