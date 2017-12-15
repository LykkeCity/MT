using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Client
{
    [PublicAPI]
    public interface IMtBackendClient
    {
        IDayOffExclusionsApi DayOffExclusions { get; }
        IScheduleSettingsApi ScheduleSettings { get; }
    }
}