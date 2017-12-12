using System;
using System.Collections.Generic;

namespace MarginTrading.Contract.BackendContracts.DayOffSettings
{
    public class ScheduleSettingsContract
    {
        public DayOfWeek DayOffStartDay { get; set; }
        public TimeSpan DayOffStartTime { get; set; }
        public DayOfWeek DayOffEndDay { get; set; }
        public TimeSpan DayOffEndTime { get; set; }
        public HashSet<string> AssetPairsWithoutDayOff { get; set; }
        public TimeSpan PendingOrdersCutOff { get; set; }
    }
}