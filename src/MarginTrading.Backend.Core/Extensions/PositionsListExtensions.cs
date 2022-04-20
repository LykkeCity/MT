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

        public static (decimal Margin, decimal Volume) SummarizeVolume(this IEnumerable<Position> source)
        {
            var positions = source.ToList();
            
            if (positions.All(p => p.Volume >= 0) || 
                positions.All(p => p.Volume <= 0))
            {
                var result = positions
                    .GroupBy(p => 1)
                    .Select(g => new
                    {
                        Margin = g.Sum(p => p.GetMarginMaintenance()),
                        Volume = g.Sum(p => Math.Abs(p.Volume))

                    })
                    .SingleOrDefault();

                return (result?.Margin ?? 0, result?.Volume ?? 0);   
            }

            throw new InvalidOperationException("Only single direction positions volume can be summarized");
        }
    }
}