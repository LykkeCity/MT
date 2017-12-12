using System;
using System.Collections.Immutable;

namespace MarginTrading.Backend.Core.DayOffSettings
{
    public class DayOffSettingsRoot
    {
        public ImmutableDictionary<Guid, DayOffExclusion> Exclusions { get; }
        public ScheduleSettings ScheduleSettings { get; }

        public DayOffSettingsRoot(ImmutableDictionary<Guid, DayOffExclusion> exclusions,
            ScheduleSettings scheduleSettings)
        {
            Exclusions = exclusions ?? throw new ArgumentNullException(nameof(exclusions));
            ScheduleSettings = scheduleSettings ?? throw new ArgumentNullException(nameof(scheduleSettings));
        }
    }
}