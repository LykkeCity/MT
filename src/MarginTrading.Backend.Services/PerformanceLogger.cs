// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services
{
    public sealed class PerformanceLogger : BackgroundService
    {
        private readonly OrdersCache _ordersCache;
        private readonly ILogger<PerformanceLogger> _logger;
        private readonly TimeSpan _loggingPeriod = TimeSpan.FromMinutes(1);

        public PerformanceLogger(OrdersCache ordersCache, ILogger<PerformanceLogger> logger)
        {
            _logger = logger;
            _ordersCache = ordersCache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(PrintPerformanceStatistics());
                _logger.LogInformation(PrintPositionsStatistics());
                
                await Task.Delay(_loggingPeriod, stoppingToken);
            }
        }

        private static string PrintPerformanceStatistics()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("=======-Performance statistics-==========");
            
            foreach (var stat in PerformanceTracker.Statistics)
            {
                var totalExecutionTimeFormatted = FormatMilliseconds(stat.Value.TotalExecutionMs);
                var averageExecutionTimeFormatted =
                    FormatMilliseconds(stat.Value.TotalExecutionMs / stat.Value.CallsCounter);
                
                var methodInfo = $"Method: {stat.Key}".PadRight(120);
                var callsInfo = $"Calls: {stat.Value.CallsCounter}".PadRight(20);
                var totalExecutionTimeInfo = $"Total execution time: {totalExecutionTimeFormatted}".PadRight(40);
                var averageExecutionTimeInfo = $"Average execution time: {averageExecutionTimeFormatted}".PadRight(40);

                sb.AppendLine($"{methodInfo} | {callsInfo} | {totalExecutionTimeInfo} | {averageExecutionTimeInfo}");
            }
            sb.AppendLine("====-Performance statistics (end)-=======");
            return sb.ToString();
        }

        private string PrintPositionsStatistics()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("=========-Positions statistics-==========");
            foreach (var positions in _ordersCache
                         .GetPositions()
                         .GroupBy(p => p.AssetPairId))
            {
                var assetInfo = $"Asset: {positions.Key}".PadRight(100);
                var countInfo = $"Count: {positions.Count()}".PadRight(20);
                
                sb.AppendLine($"{assetInfo} | {countInfo}");
            }
            sb.AppendLine("======-Positions statistics (end)-=======");
            return sb.ToString();
        }

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
    }
}