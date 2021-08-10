// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Helpers
{
    public static class PaginationHelper
    {
        public const int MaxResults = 1000;
        public const int UnspecifiedResults = 20;

        public static int GetTake(int? take)
        {
            return take == null
                ? UnspecifiedResults
                : Math.Min(take.Value, MaxResults);
        }
    }
}