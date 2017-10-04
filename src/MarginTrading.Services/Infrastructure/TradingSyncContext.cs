using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MarginTrading.Core.Settings;
using MarginTrading.Core.Telemetry;
using MarginTradingHelpers = MarginTrading.Services.Helpers.MarginTradingHelpers;

namespace MarginTrading.Services.Infrastructure
{
    /// <summary>
    /// Context for synchronization of trade operations.
    /// Usage of async/await inside context is prohibited:
    /// under the hood it uses the Monitor.Enter() & Exit(), which are broken by awaits.
    /// </summary>    
    ///     
    /// <remarks>
    /// If async continuation happens to execute on a different thread - 
    /// monitor will not be able to perform Exit(), and nested Enter() will lead to a deadlock.
    /// This is because monitor tracks the thread which entered the lock.
    /// Such code can work on dev environment, but can cause "magic" issues on production.
    /// </remarks>
    public class TradingSyncContext : IDisposable
    {
        private readonly string _contextType;
        private readonly string _contextSource;
        private readonly ITelemetryPublisher _telemetryPublisher;
        private readonly MarginSettings _marginTradingSettings;
        private static int _contextNestingDepth;
        private readonly long _waitingForLockTime;

        private readonly Stopwatch _sw = new Stopwatch();

        public TradingSyncContext(string contextType, string contextSource,
            ITelemetryPublisher telemetryPublisher, MarginSettings marginTradingSettings)
        {
            _contextType = contextType;
            _contextSource = contextSource;
            _telemetryPublisher = telemetryPublisher;
            _marginTradingSettings = marginTradingSettings;

            _sw.Start();

            Monitor.Enter(MarginTradingHelpers.TradingMatchingSync);

            _waitingForLockTime = _sw.ElapsedMilliseconds;

            _sw.Restart();
            _contextNestingDepth++;
        }

        public void Dispose()
        {
            var depth = _contextNestingDepth;
            _contextNestingDepth--;
            _sw.Stop();
            Monitor.Exit(MarginTradingHelpers.TradingMatchingSync);

            var processingTime = _sw.ElapsedMilliseconds;

            if (_marginTradingSettings.Telemetry != null 
                && (_waitingForLockTime >= _marginTradingSettings.Telemetry.LockMetricThreshold
                || processingTime >= _marginTradingSettings.Telemetry.LockMetricThreshold))
            {
                _telemetryPublisher.PublishEventMetrics(_contextType, _contextSource,
                    new Dictionary<string, double>
                    {
                        {TelemetryConstants.ContextDepthPropName, depth},
                        {TelemetryConstants.PendingTimePropName, _waitingForLockTime},
                        {TelemetryConstants.ProcessingTimePropName, _sw.ElapsedMilliseconds}
                    });
            }
        }
    }
}
