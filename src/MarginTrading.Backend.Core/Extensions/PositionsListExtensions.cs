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
        private struct PositionsAccumulator
        {
            public decimal Margin { get; set; }
            public decimal Volume { get; set; }
        }
        
        public static IOrderedEnumerable<Position> LargestPnlFirst(this IEnumerable<Position> source) =>
            source.OrderByDescending(p => p.GetUnrealisedPnl());

        public static (decimal Margin, decimal Volume) SummarizeVolume(this IEnumerable<Position> source)
        {
            var positions = source.ToList();
            
            if (positions.All(p => p.Volume >= 0) || 
                positions.All(p => p.Volume <= 0))
            {
                var accumulator = positions.Aggregate(new PositionsAccumulator(), (a, nextPosition) =>
                {
                    a.Margin += nextPosition.GetMarginMaintenance();
                    a.Volume += Math.Abs(nextPosition.Volume);
                    return a;
                });

                return (accumulator.Margin, accumulator.Volume);   
            }

            throw new InvalidOperationException("Only single direction positions volume can be summarized");
        }
    }
}