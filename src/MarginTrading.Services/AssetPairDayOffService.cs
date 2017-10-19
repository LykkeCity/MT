using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Core;
using MarginTrading.Core.Assets;
using MarginTrading.Core.Settings;

namespace MarginTrading.Services
{
    public class AssetPairDayOffService : IAssetPairDayOffService
    {
        private readonly IDateService _dateService;
        private readonly ScheduleSettings _scheduleSettings;
        private readonly HashSet<string> _assetPairsWithoutDayOff;

        public AssetPairDayOffService(IDateService dateService,
            ScheduleSettings scheduleSettings)
        {
            _dateService = dateService;
            _scheduleSettings = scheduleSettings;
            _assetPairsWithoutDayOff = _scheduleSettings.AssetPairsWithoutDayOff?.ToHashSet() ?? new HashSet<string>();
        }

        public bool IsDayOff(string assetPairId)
        {
            return IsDayOff(assetPairId, TimeSpan.Zero);
        }
        
        public bool IsPendingOrderDisabled(string assetPairId)
        {
            return IsDayOff(assetPairId, _scheduleSettings.PendingOrdersCutOff);
        }

        public bool IsPendingOrdersDisabledTime()
        {
            return IsNowNotInSchedule(_scheduleSettings.PendingOrdersCutOff);
        }

        public bool IsAssetPairHasNoDayOff(string assetPairId)
        {
            return _assetPairsWithoutDayOff.Contains(assetPairId);
        }

        private bool IsDayOff(string assetPairId, TimeSpan cutOff)
        {
            if (IsAssetPairHasNoDayOff(assetPairId))
                return false;

            return IsNowNotInSchedule(cutOff);
        }
        
        /// <summary>
        /// Check if current time is not in schedule
        /// </summary>
        /// <param name="scheduleCutOff">
        /// Timespan to reduce schedule from both sides
        /// </param>
        /// <returns></returns>
        private bool IsNowNotInSchedule(TimeSpan scheduleCutOff)
        {
            var currentDateTime = _dateService.Now();
            
            var closestDayOffStart = GetNextWeekday(currentDateTime, _scheduleSettings.DayOffStartDay)
                                    .Add(_scheduleSettings.DayOffStartTime.Subtract(scheduleCutOff));

            var closestDayOffEnd = GetNextWeekday(currentDateTime, _scheduleSettings.DayOffEndDay)
                                    .Add(_scheduleSettings.DayOffStartTime.Add(scheduleCutOff));

            if (closestDayOffStart > closestDayOffEnd)
            {
                closestDayOffStart = closestDayOffEnd.AddDays(-7);
            }

            return currentDateTime >= closestDayOffStart && currentDateTime < closestDayOffEnd;
        }
        
        private static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            var daysToAdd = ((int) day - (int) start.DayOfWeek + 7) % 7;
            return start.Date.AddDays(daysToAdd);
        }
    }
}
