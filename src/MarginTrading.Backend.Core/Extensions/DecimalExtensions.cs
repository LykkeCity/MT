// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Extensions
{
    public static class DecimalExtensions
    {
        public static bool EqualsZero(this decimal source)
        {
            return decimal.Compare(decimal.Zero, source) == 0;
        }

        public static decimal WithAccuracy(this decimal value, int accuracy)
        {
            return Math.Round(value, accuracy);
        }

        public static decimal WithDefaultAccuracy(this decimal value)
        {
            return value.WithAccuracy(AssetsConstants.DefaultAssetAccuracy);
        }
    }
}