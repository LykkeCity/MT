// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Services.Helpers
{
    public static class MarginTradingCalculations
    {
        public static decimal GetVolumeFromPoints(decimal amount, int accuracy)
        {
            var val = Math.Pow(10, -accuracy);
            return (decimal) val * amount;
        }
    }
}
