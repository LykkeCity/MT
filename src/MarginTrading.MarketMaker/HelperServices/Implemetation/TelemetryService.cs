using System.Collections.Generic;
using Microsoft.ApplicationInsights;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    public class TelemetryService : ITelemetryService
    {
        private readonly TelemetryClient _telemetryClient;

        private const string SignalSourcePropName = "SignalSource";


        public TelemetryService()
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
