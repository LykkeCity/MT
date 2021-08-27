// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Extensions
{
    public static class PositionsListExtensions
    {
        public static IOrderedEnumerable<Position> LargestPnlFirst(this IEnumerable<Position> source) =>
            source.OrderByDescending(p => p.GetUnrealisedPnl());
    }
}