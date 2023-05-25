// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Services
{
    internal static class PerformanceTracker
    {
        public struct MethodStatistics
        {
            public MethodStatistics(int callsCounter, long totalExecutionMs)
            {
                CallsCounter = callsCounter;
                TotalExecutionMs = totalExecutionMs;
            }

            public int CallsCounter { get; }
            public long TotalExecutionMs { get; }
        }
        
        public static readonly ConcurrentDictionary<string, MethodStatistics> Statistics =
            new ConcurrentDictionary<string, MethodStatistics>();

        public static void Track(string methodName,
            Action action,
            [CanBeNull] string assetPair = null)
        {
            var key = GetKey(methodName, assetPair);

            var t = TrackInternalAsync(key, () =>
            {
                action();
                return Task.CompletedTask;
            });

            t.GetAwaiter().GetResult();
        }
        
        public static Task TrackAsync(string methodName,
            Func<Task> action,
            [CanBeNull] string assetPair = null)
        {
            var key = GetKey(methodName, assetPair);

            return TrackInternalAsync(key, action);
        }

        private static async Task TrackInternalAsync(string key, Func<Task> action)
        {
            var watch = Stopwatch.StartNew();

            await action();
            
            watch.Stop();
            
            var elapsedMs = watch.ElapsedMilliseconds;

            Statistics.AddOrUpdate(key, new MethodStatistics(1, elapsedMs),
                (_, stats) => new MethodStatistics(stats.CallsCounter + 1, stats.TotalExecutionMs + elapsedMs));
        }

        private static string GetKey(string methodName, [CanBeNull] string assetPair)
        {
            var assetPairStr = assetPair ?? "AssetPairNotApplicable";
            return $"{methodName}:{assetPairStr}";   
        }
    }
}