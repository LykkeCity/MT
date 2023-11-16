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
            _logger.LogInformation("Performance logger started");
            
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
                var line = PerformanceInfoFormatter.FormatMethodStatistics(stat.Key, stat.Value);
                sb.AppendLine(line);
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
                var line = PerformanceInfoFormatter.FormatPositionStatistics(positions.Key, positions.Count());
                sb.AppendLine(line);
            }
            sb.AppendLine("======-Positions statistics (end)-=======");
            return sb.ToString();
        }
    }
}