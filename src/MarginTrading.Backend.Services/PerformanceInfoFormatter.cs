// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Services
{
    internal static class PerformanceInfoFormatter
    {
        private static string FormatMilliseconds(long milliseconds)
        {
            var time = TimeSpan.FromMilliseconds(milliseconds);
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

            return formattedTime;
        }
        
        public static string FormatMethodStatistics(string methodKey, PerformanceTracker.MethodStatistics stat)
        {
            var totalExecutionTimeFormatted = FormatMilliseconds(stat.TotalExecutionMs);
            var averageExecutionTimeFormatted = FormatMilliseconds(stat.TotalExecutionMs / stat.CallsCounter);
            
            var methodInfo = $"Method: {methodKey}".PadRight(120);
            var callsInfo = $"Calls: {stat.CallsCounter}".PadRight(20);
            var totalExecutionTimeInfo = $"Total execution time: {totalExecutionTimeFormatted}".PadRight(40);
            var averageExecutionTimeInfo = $"Average execution time: {averageExecutionTimeFormatted}".PadRight(40);

            return $"{methodInfo} | {callsInfo} | {totalExecutionTimeInfo} | {averageExecutionTimeInfo}";
        }

        public static string FormatPositionStatistics(string assetPairId, int counter)
        {
            var assetInfo = $"Asset: {assetPairId}".PadRight(100);
            var countInfo = $"Count: {counter}".PadRight(20);
                
            return $"{assetInfo} | {countInfo}";
        }
    }
}