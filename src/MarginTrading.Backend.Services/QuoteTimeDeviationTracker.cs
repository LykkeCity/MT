// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;

namespace MarginTrading.Backend.Services
{
    internal static class QuoteTimeDeviationTracker
    {
        public struct QuoteTimeDeviation
        {
            public TimeSpan Value { get; }
            public QuoteTimeDeviation(DateTime now, DateTime quoteTime)
            {
                Value = now - quoteTime;
                
                if (Value.TotalMilliseconds < 0)
                {
                    throw new ArgumentException("Quote time can't be in the future", nameof(quoteTime));
                }
            }
        }
        
        public struct QuoteTimeDeviationAccumulator
        {
            public QuoteTimeDeviationAccumulator(int quotesCounter, double totalDeviationMs)
            {
                QuotesCounter = quotesCounter;
                TotalDeviationMs = totalDeviationMs;
            }

            public int QuotesCounter { get; }
            public double TotalDeviationMs { get; }
        }
        
        private static readonly ConcurrentDictionary<string, QuoteTimeDeviationAccumulator> Statistics =
            new ConcurrentDictionary<string, QuoteTimeDeviationAccumulator>();

        public static void Track(string assetPairId, QuoteTimeDeviation deviation)
        {
            Statistics.AddOrUpdate(assetPairId, 
                new QuoteTimeDeviationAccumulator(1, deviation.Value.TotalMilliseconds),
                (_, v) => 
                    new QuoteTimeDeviationAccumulator(v.QuotesCounter + 1, v.TotalDeviationMs + deviation.Value.TotalMilliseconds));
        }

        public static ConcurrentDictionary<string, QuoteTimeDeviationAccumulator> Flush()
        {
            var copy = new ConcurrentDictionary<string, QuoteTimeDeviationAccumulator>(Statistics);
            Statistics.Clear();
            
            return copy;
        }
    }
}