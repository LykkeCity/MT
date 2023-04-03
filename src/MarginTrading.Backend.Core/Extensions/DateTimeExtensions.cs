// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime MaxDateTime(DateTime first, DateTime second)
        {
            if (Comparer<DateTime>.Default.Compare(first, second) > 0)
                return first;

            return second;
        }
    }
}