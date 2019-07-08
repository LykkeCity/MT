// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.Settings
{
    /// <summary>
    /// Telementry settings
    /// </summary>
    public class TelemetrySettings
    {
        /// <summary>
        /// Minimal duration of lock in ms to send event to telemetry
        /// </summary>
        public int LockMetricThreshold { get; set; }
    }
}