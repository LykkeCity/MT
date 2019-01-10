using MarginTrading.Backend.Core.DayOffSettings;

namespace MarginTrading.Backend.Core.Extensions
{
    public static class ScheduleSettingsExtensions
    {
        public static bool Enabled(this CompiledScheduleTimeInterval compiledScheduleTimeInterval)
        {
            return compiledScheduleTimeInterval.Schedule.IsTradeEnabled ?? true;
        }
    }
}