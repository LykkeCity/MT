using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Services;
// ReSharper disable PossibleInvalidOperationException

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
        /// <param name="scheduleCutOff">Timespan to reduce schedule from both sides</param>
        /// <returns></returns>
        private bool IsNowNotInSchedule(string assetPairId, TimeSpan scheduleCutOff)
        {
            var currentDateTime = _dateService.Now();

            var schedule = _scheduleSettingsCacheService.GetCompiledScheduleSettings(assetPairId, 
                currentDateTime, scheduleCutOff);

            return !_scheduleSettingsCacheService.GetTradingEnabled(schedule);
        }
    }
}
