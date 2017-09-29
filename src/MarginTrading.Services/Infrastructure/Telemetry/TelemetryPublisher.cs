using System.Collections.Generic;
using MarginTrading.Core.Telemetry;
using Microsoft.ApplicationInsights;

namespace MarginTrading.Services.Infrastructure.Telemetry
{
    public class TelemetryPublisher : ITelemetryPublisher
    {
        private readonly TelemetryClient _telemetryClient;

        private const string SignalSourcePropName = "SignalSource";


        public TelemetryPublisher()
        {
            _telemetryClient = new TelemetryClient();
        }

        public void PublishEventMetrics(string eventName, string signalSource, IDictionary<string, double> metrics,
            IDictionary<string, string> additionalProperties = null)
        {
            var properties = additionalProperties ?? new Dictionary<string, string>();
            properties.Add(SignalSourcePropName, signalSource);

            _telemetryClient.TrackEvent(eventName, properties, metrics);
        }
    }
}
