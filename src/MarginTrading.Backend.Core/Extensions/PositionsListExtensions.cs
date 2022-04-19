// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Extensions
{
    public static class PositionsListExtensions
    {
        public static IOrderedEnumerable<Position> LargestPnlFirst(this IEnumerable<Position> source) =>
            source.OrderByDescending(p => p.GetUnrealisedPnl());

        public static (decimal Margin, decimal Volume) Summarize(this IEnumerable<Position> source) 
        {
            var result = source
                .GroupBy(p => 1)
                .Select(g => new
                {
                    Margin = g.Sum(p => p.GetMarginMaintenance()),
                    Volume = g.Sum(p => Math.Abs(p.Volume))

                }).Single();

            return (result.Margin, result.Volume);
        }
    }
}