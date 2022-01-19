// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Prices
{
    /// <summary>
    /// Closing asset price
    /// </summary>
    [PublicAPI]
    public class ClosingAssetPrice
    {
        public string MdsCode { get; set; }

        public decimal ClosePrice { get; set; }
    }
}