// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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