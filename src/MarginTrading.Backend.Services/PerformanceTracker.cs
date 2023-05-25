// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Common.Log;

namespace MarginTrading.Backend.Services
{
    internal static class PerformanceTracker
    {
        private static readonly ConcurrentDictionary<string, int> MethodCounter =
            new ConcurrentDictionary<string, int>();

        public static void Track(string methodName, Action action, ILog logger)
        {
            var t = TrackInternalAsync(methodName, () =>
            {
                action();
                return Task.CompletedTask;
            }, logger);

            t.GetAwaiter().GetResult();
        }
        
        public static Task TrackAsync(string methodName, Func<Task> action, ILog logger)
        {
            return TrackInternalAsync(methodName, action, logger);
        }

        private static async Task TrackInternalAsync(string methodName, Func<Task> action, ILog logger)
        {
            var watch = Stopwatch.StartNew();
            
            MethodCounter.AddOrUpdate(methodName, 1, (_, count) => count + 1);

            await action();
            
            watch.Stop();
            
            var elapsedMs = watch.ElapsedMilliseconds;
            var callsCounter = MethodCounter[methodName];
            
            Log(methodName, elapsedMs, callsCounter, logger);
        }
        
        private static void Log(string methodName, long elapsedMs, int callsCounter, ILog logger)
        {
            var time = TimeSpan.FromMilliseconds(elapsedMs);
            string formattedTime;
            if (time.TotalSeconds < 1)
            {
                formattedTime = $"{time.TotalMilliseconds:0.##} ms";
            }
            else if (time.TotalMinutes < 1)
            {
                formattedTime = $"{time.TotalSeconds:0.##} sec";
            }
            else if (time.TotalHours < 1)
            {
                formattedTime = $"{time.TotalMinutes:0.##} min";
            }
            else
            {
                formattedTime = $"{time.TotalHours:0.##} hours";
            }

            var message =
                $"[Performance tracker]: Method {methodName} took {formattedTime}. Calls count {callsCounter}";
            logger.WriteInfo(nameof(PerformanceTracker), null, message);
        }
    }
}