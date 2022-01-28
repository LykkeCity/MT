// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Prices
{
    /// <summary>
    /// Closing fx rate
    /// </summary>
    [PublicAPI]
    public class ClosingFxRate
    {
        public string AssetId { get; set; }

        public decimal ClosePrice { get; set; }
    }
}