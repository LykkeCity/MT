using System;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using Lykke.SettingsReader;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class AlertSeverityLevelService : IAlertSeverityLevelService
    {
        private readonly IReloadingManager<ReadOnlyCollection<(EventTypeEnum Event, string SlackChannelType)>> _levels;

        public AlertSeverityLevelService(IReloadingManager<RiskInformingSettings> settings)
        {
            _levels = settings.Nested(s =>
            {
                return s.Data.Where(d => d.System == "QuotesMonitor")
                    .Select(d => (ConvertEventTypeCode(d.EventTypeCode), ConvertLevel(d.Level)))
                    .ToList().AsReadOnly();
            });
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
        private static string ConvertLevel(AlertSeverityLevel alertSeverityLevel)
        {
            switch (alertSeverityLevel)
            {
                case AlertSeverityLevel.None:
                    return null;
                case AlertSeverityLevel.Information:
                    return "mt-information";
                case AlertSeverityLevel.Warning:
                    return "mt-warning";
                case AlertSeverityLevel.Critical:
                    return "mt-critical";
                default:
                    throw new ArgumentOutOfRangeException(nameof(RiskInformingParams.Level), alertSeverityLevel, null);
            }
        }

        public string GetSlackChannelType(EventTypeEnum eventType)
        {
            return _levels.CurrentValue.Single(l => l.Event == eventType).SlackChannelType;
        }
    }
}