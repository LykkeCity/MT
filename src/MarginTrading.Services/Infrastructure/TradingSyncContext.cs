﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MarginTrading.Core.Settings;
using MarginTrading.Core.Telemetry;
using MarginTradingHelpers = MarginTrading.Services.Helpers.MarginTradingHelpers;

namespace MarginTrading.Services.Infrastructure
{
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
