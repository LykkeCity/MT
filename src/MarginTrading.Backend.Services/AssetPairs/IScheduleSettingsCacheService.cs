using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.DayOffSettings;

namespace MarginTrading.Backend.Services.AssetPairs
{
    public interface IScheduleSettingsCacheService
    {
        Dictionary<string, List<CompiledScheduleTimeInterval>> GetCompiledScheduleSettings(DateTime currentDateTime,
            TimeSpan scheduleCutOff);
        
        /// <summary>
        /// Get compiled schedule timeline from cache, recalculate it if needed.
        /// </summary>
        List<CompiledScheduleTimeInterval> GetCompiledScheduleSettings(string assetPairId,
            DateTime currentDateTime, TimeSpan scheduleCutOff);

        void CacheWarmUp();

        void PlatformCacheWarmUp();

        Task UpdateAllSettingsAsync();
        
        Task UpdateSettingsAsync();

        Task UpdatePlatformSettingsAsync();

        /// <summary>
        /// Get current and next day time intervals of the platform disablement hours.
        /// </summary>
        List<CompiledScheduleTimeInterval> GetPlatformTradingSchedule();

        bool GetPlatformTradingEnabled();

        bool AssetPairTradingEnabled(string assetPairId, TimeSpan scheduleCutOff);
    }
}