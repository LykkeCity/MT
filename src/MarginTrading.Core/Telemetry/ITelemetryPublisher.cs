using System.Collections.Generic;

namespace MarginTrading.Core.Telemetry
{
    public interface ITelemetryPublisher
    {
        /// <summary>
        /// Publish metrics of some action
        /// </summary>
        /// <param name="eventName">Name of event from <see cref="TelemetryConstants"/></param>
        /// <param name="signalSource">Name of process where event was started</param>
        /// <param name="metrics">Collection of event metrics</param>
        /// <param name="additionalProperties">Collection of additional event properties</param>
        void PublishEventMetrics(string eventName, string signalSource, IDictionary<string, double> metrics, IDictionary<string, string> additionalProperties = null);
    }
}
