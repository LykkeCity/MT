using System;
using System.Linq;
using JetBrains.Annotations;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.AssetPairs
{
    public class AssetPairDayOffService : IAssetPairDayOffService
    {
        private readonly IDateService _dateService;
        private readonly IDayOffSettingsService _dayOffSettingsService;

        public AssetPairDayOffService(IDateService dateService, IDayOffSettingsService dayOffSettingsService)
        {
            _dateService = dateService;
            _dayOffSettingsService = dayOffSettingsService;
        }

        public bool IsDayOff(string assetPairId)
        {
            return IsNowNotInSchedule(assetPairId, TimeSpan.Zero);
        }
        
        public bool ArePendingOrdersDisabled(string assetPairId)
        {
            return IsNowNotInSchedule(assetPairId, _dayOffSettingsService.GetScheduleSettings().PendingOrdersCutOff);
        }

        /// <summary>
        /// Check if current time is not in schedule
        /// </summary>
        /// <param name="assetPairId"></param>
        /// <param name="scheduleCutOff">
        ///     Timespan to reduce schedule from both sides
        /// </param>
        /// <returns></returns>
        private bool IsNowNotInSchedule(string assetPairId, TimeSpan scheduleCutOff)
        {
            var currentDateTime = _dateService.Now();
            var isDayOffByExclusion = IsDayOffByExclusion(assetPairId, scheduleCutOff, currentDateTime);
            if (isDayOffByExclusion != null)
                return isDayOffByExclusion.Value; 
            
            var scheduleSettings = _dayOffSettingsService.GetScheduleSettings();
            if (scheduleSettings.AssetPairsWithoutDayOff.Contains(assetPairId))
                return false;
            
            var closestDayOffStart = GetNextWeekday(currentDateTime, scheduleSettings.DayOffStartDay)
                                    .Add(scheduleSettings.DayOffStartTime.Subtract(scheduleCutOff));

            var closestDayOffEnd = GetNextWeekday(currentDateTime, scheduleSettings.DayOffEndDay)
                                    .Add(scheduleSettings.DayOffEndTime.Add(scheduleCutOff));

            if (closestDayOffStart > closestDayOffEnd)
            {
                closestDayOffStart = closestDayOffEnd.AddDays(-7);
            }

            return (currentDateTime >= closestDayOffStart && currentDateTime < closestDayOffEnd)
                   //don't even try to understand
                   || currentDateTime < closestDayOffEnd.AddDays(-7);
        }

        [CanBeNull]
        private bool? IsDayOffByExclusion(string assetPairId, TimeSpan scheduleCutOff, DateTime currentDateTime)
        {
            var dayOffExclusions = _dayOffSettingsService.GetExclusions(assetPairId);
            return dayOffExclusions
                .Where(e =>
                {
                    var start = e.IsTradeEnabled ? e.Start.Add(scheduleCutOff) : e.Start.Subtract(scheduleCutOff);
                    var end = e.IsTradeEnabled ? e.End.Subtract(scheduleCutOff) : e.End.Add(scheduleCutOff);
                    return IsBetween(currentDateTime, start, end);
                }).DefaultIfEmpty()
                .Select(e => e == null ? (bool?) null : !e.IsTradeEnabled).Max();
        }

        private static bool IsBetween(DateTime currentDateTime, DateTime start, DateTime end)
        {
            return start <= currentDateTime && currentDateTime <= end;
        }

        private static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            var daysToAdd = ((int) day - (int) start.DayOfWeek + 7) % 7;
            return start.Date.AddDays(daysToAdd);
        }
    }
}
