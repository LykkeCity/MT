using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.AssetPairs
{
    public class AssetPairDayOffService : IAssetPairDayOffService
    {
        private readonly IDateService _dateService;
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;

        public AssetPairDayOffService(IDateService dateService, IScheduleSettingsCacheService scheduleSettingsCacheService)
        {
            _dateService = dateService;
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
        }

        public bool IsDayOff(string assetPairId)
        {
            return IsNowNotInSchedule(assetPairId, TimeSpan.Zero);
        }
        
        public bool ArePendingOrdersDisabled(string assetPairId)
        {
            //TODO TBD in https://lykke-snow.atlassian.net/browse/MTC-155
            return false; //IsNowNotInSchedule(assetPairId, _dayOffSettingsService.GetScheduleSettings().PendingOrdersCutOff);
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
            
            var scheduleSettings = _scheduleSettingsCacheService.GetScheduleSettings(assetPairId);
            
            //get recurring closest intervals
            var recurring = scheduleSettings
                .Where(x => x.Start.Date == null)//validated in settings
                .SelectMany(sch =>
                {
                    var currentStart = GetCurrentWeekday(currentDateTime, sch.Start.DayOfWeek.Value)
                        .Add(sch.Start.Time.Subtract(scheduleCutOff));
                    var currentEnd = GetCurrentWeekday(currentDateTime, sch.End.DayOfWeek.Value)
                        .Add(sch.End.Time.Add(scheduleCutOff));
                    if (currentEnd < currentStart)
                    {
                        currentEnd = currentEnd.AddDays(7);
                    }
                     
                    return new []
                    {
                        (sch, currentStart, currentEnd),
                        (sch, currentStart.AddDays(-7), currentEnd.AddDays(-7))
                    };
                });
            //get separate intervals
            var separate = scheduleSettings
                .Where(x => x.Start.Date != null)
                .Select(sch => (sch, sch.Start.Date.Value.Add(sch.Start.Time.Subtract(scheduleCutOff)),
                    sch.End.Date.Value.Add(sch.End.Time.Add(scheduleCutOff))));
            //TODO probably we can cache it for some time.. if needed.

            var intersecting = recurring.Concat(separate).Where(x => IsBetween(currentDateTime, x.Item2, x.Item3));

            return !(intersecting.OrderByDescending(x => x.sch.Rank)
                         .Select(x => x.sch).FirstOrDefault()?.IsTradeEnabled ?? true);
        }

        private static bool IsBetween(DateTime currentDateTime, DateTime start, DateTime end)
        {
            return start <= currentDateTime && currentDateTime <= end;
        }

        private static DateTime GetCurrentWeekday(DateTime start, DayOfWeek day)
        {
            var daysToAdd = ((int) day - (int) start.DayOfWeek) % 7;
            return start.Date.AddDays(daysToAdd);
        }
    }
}
