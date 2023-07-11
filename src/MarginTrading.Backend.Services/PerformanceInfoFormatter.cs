// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Services
{
    internal static class PerformanceInfoFormatter
    {
        public static string FormatMethodStatistics(string methodKey, PerformanceTracker.MethodStatistics stat)
        {
            var totalExecutionTimeFormatted = FormattingUtils.FormatMilliseconds(stat.TotalExecutionMs);
            var averageExecutionTimeFormatted =
                FormattingUtils.FormatMilliseconds(stat.TotalExecutionMs / stat.CallsCounter);
            
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