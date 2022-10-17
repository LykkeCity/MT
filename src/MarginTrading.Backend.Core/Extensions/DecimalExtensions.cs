// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.Extensions
{
    public static class DecimalExtensions
    {
        public static bool EqualsZero(this decimal source)
        {
            return decimal.Compare(decimal.Zero, source) == 0;
        }
    }
}