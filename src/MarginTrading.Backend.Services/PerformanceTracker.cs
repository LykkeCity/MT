// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Services
{
    public static class PerformanceTracker
    {
        public struct MethodIdentity
        {
            public string Owner { get; }
            public string Name { get; }
            [CanBeNull] public string Parameter { get; }
            
            public MethodIdentity(string owner, string name, [CanBeNull] string parameter)
            {
                if (string.IsNullOrEmpty(owner))
                    throw new ArgumentException("Value cannot be null or empty.", nameof(owner));
                
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException("Value cannot be null or empty.", nameof(name));
                
                Owner = owner;
                Name = name;
                Parameter = parameter;
            }

            public override string ToString()
            {
                return string.IsNullOrEmpty(Parameter)
                    ? $"{Owner}:{Name}"
                    : $"{Owner}:{Name}:{Parameter}";
            }
        }
        
        public struct MethodStatistics
        {
            public MethodStatistics(int callsCounter, long totalExecutionMs, long maxExecutionMs)
            {
                CallsCounter = callsCounter;
                TotalExecutionMs = totalExecutionMs;
                MaxExecutionMs = maxExecutionMs;
            }

            public int CallsCounter { get; }
            public long TotalExecutionMs { get; }
            
            public long MaxExecutionMs { get; }
        }
        
        public static readonly ConcurrentDictionary<MethodIdentity, MethodStatistics> Statistics =
            new ConcurrentDictionary<MethodIdentity, MethodStatistics>();
        
        public static bool Enabled { get; set; }

        public static void Track(MethodIdentity methodIdentity, Action action)
        {
            var t = TrackInternalAsync(methodIdentity, () =>
            {
                action();
                return Task.CompletedTask;
            });

            t.GetAwaiter().GetResult();
        }
        
        public static Task TrackAsync(MethodIdentity methodIdentity, Func<Task> action)
        {
            return TrackInternalAsync(methodIdentity, action);
        }

        private static async Task TrackInternalAsync(MethodIdentity methodIdentity, Func<Task> action)
        {
            if (!Enabled)
            {
                await action();
                return;
            }
            
            var watch = Stopwatch.StartNew();

            await action();
            
            watch.Stop();
            
            var elapsedMs = watch.ElapsedMilliseconds;

            Statistics.AddOrUpdate(methodIdentity, new MethodStatistics(1, elapsedMs, elapsedMs),
                (_, stats) => new MethodStatistics(
                    stats.CallsCounter + 1,
                    stats.TotalExecutionMs + elapsedMs,
                    elapsedMs > stats.MaxExecutionMs ? elapsedMs : stats.MaxExecutionMs));
        }
    }
}