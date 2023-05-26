// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services
{
    public sealed class QuotesDeviationLogger : BackgroundService
    {
        private readonly ILogger<QuotesDeviationLogger> _logger;
        private readonly TimeSpan _loggingPeriod = TimeSpan.FromMinutes(1);
        
        public QuotesDeviationLogger(ILogger<QuotesDeviationLogger> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var statistics = QuoteTimeDeviationTracker.Flush();
                var formattedStatistics = PrintQuotesDeviationStatistics(statistics);
                _logger.LogInformation(formattedStatistics);
                
                await Task.Delay(_loggingPeriod, stoppingToken);
            }
        }

        private static string PrintQuotesDeviationStatistics(
            ConcurrentDictionary<string, QuoteTimeDeviationTracker.QuoteTimeDeviationAccumulator> statistics)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("=======-Quotes deviation statistics-==========");
            foreach (var stat in statistics)
            {
                var line = QuoteDeviationInfoFormatter.FormatDeviationStatistics(stat.Key, stat.Value);
                sb.AppendLine(line);
            }

            sb.AppendLine("=====-Quotes deviation statistics (end)-=======");
            return sb.ToString();
        }
    }
}