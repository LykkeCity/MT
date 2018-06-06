using System;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Settings;
using MoreLinq;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class AlertSeverityLevelService : IAlertSeverityLevelService
    {
        private readonly ReadOnlyCollection<(EventTypeEnum Event, string SlackChannelType)> _levels;
        
        private static readonly string _defaultLevel = "mt-critical";

        public AlertSeverityLevelService(RiskInformingSettings settings)
        {
            _levels = settings.Data.Where(d => d.System == "QuotesMonitor")
                .Select(d => (ConvertEventTypeCode(d.EventTypeCode), ConvertLevel(d.Level)))
                .ToList().AsReadOnly();
        }

        private static EventTypeEnum ConvertEventTypeCode(string eventTypeCode)
        {
            switch (eventTypeCode)
            {
                case "BE01": return EventTypeEnum.QuoteStopped;
                case "BE02": return EventTypeEnum.QuoteStarted;
                default:
                    throw new ArgumentOutOfRangeException(nameof(RiskInformingParams.EventTypeCode), eventTypeCode, null);
            }
        }

        [CanBeNull]
        private static string ConvertLevel(string alertSeverityLevel)
        {
            switch (alertSeverityLevel)
            {
                case "None":
                    return null;
                case "Information":
                    return "mt-information";
                case "Warning":
                    return "mt-warning";
                default:
                    return _defaultLevel;
            }
        }

        public string GetSlackChannelType(EventTypeEnum eventType)
        {
            return _levels != null
                ? _levels.Where(l => l.Event == eventType).Select(l => l.SlackChannelType)
                    .FallbackIfEmpty(_defaultLevel).Single()
                : _defaultLevel;
        }
    }
}