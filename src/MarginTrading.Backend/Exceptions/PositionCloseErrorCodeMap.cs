// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Exceptions
{
    internal static class PositionCloseErrorCodeMap
    {
        /// <summary>
        /// Maps trading disabled reason to position close error code
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static PositionCloseError Map(InstrumentTradingDisabledReason source) =>
            source switch
            {
                InstrumentTradingDisabledReason.InstrumentTradingDisabled =>
                    PositionCloseError.InstrumentTradingDisabled,
                _ => PositionCloseError.TradesDisabled
            };
    }
}