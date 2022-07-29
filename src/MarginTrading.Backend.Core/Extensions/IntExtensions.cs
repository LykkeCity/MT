// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.Extensions
{
    public static class IntExtensions
    {
        public static long ZeroIfNegative(this long source)
        {
            return source < 0 ? 0 : source;
        }
        
        public static long ZeroIfNegative(this long? source)
        {
            return source?.ZeroIfNegative() ?? 0;
        }
    }
}