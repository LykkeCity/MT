using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Core.DayOffSettings
{
    public class ScheduleSettings
    {
        public DayOfWeek DayOffStartDay { get; }
        public TimeSpan DayOffStartTime { get; }
        public DayOfWeek DayOffEndDay { get; }
        public TimeSpan DayOffEndTime { get; }
        public HashSet<string> AssetPairsWithoutDayOff { get; }
        public TimeSpan PendingOrdersCutOff { get; }

        public ScheduleSettings(DayOfWeek dayOffStartDay, TimeSpan dayOffStartTime, DayOfWeek dayOffEndDay,
            TimeSpan dayOffEndTime, HashSet<string> assetPairsWithoutDayOff, TimeSpan pendingOrdersCutOff)
        {
            DayOffStartDay = dayOffStartDay;
            DayOffStartTime = dayOffStartTime;
            DayOffEndDay = dayOffEndDay;
            DayOffEndTime = dayOffEndTime;
            AssetPairsWithoutDayOff = assetPairsWithoutDayOff ??
                                      throw new ArgumentNullException(nameof(assetPairsWithoutDayOff));
            PendingOrdersCutOff = pendingOrdersCutOff;
        }
    }
}