// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Contracts.Snow.Prices;

namespace MarginTrading.Backend.Contracts.Prices
{
    public static class PricesExtensions
    {
        public static BestPriceContract ToContract(this ClosingAssetPrice src, DateTime timestamp) =>
            new BestPriceContract
            {
                Ask = src.ClosePrice,
                Bid = src.ClosePrice,
                Id = src.MdsCode,
                Timestamp = timestamp
            };

        public static BestPriceContract ToContract(this ClosingFxRate src, DateTime timestamp) =>
            new BestPriceContract
            {
                Ask = src.ClosePrice,
                Bid = src.ClosePrice,
                Id = src.FhQuoterCode,
                Timestamp = timestamp
            };
    }
}