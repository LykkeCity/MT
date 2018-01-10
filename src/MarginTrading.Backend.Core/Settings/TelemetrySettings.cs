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