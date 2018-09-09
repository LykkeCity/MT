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
        /// <summary>
        /// Get a compiled schedule timeline from cache, recalculate it if needed.
        /// </summary>
        List<(ScheduleSettings Schedule, DateTime Start, DateTime End)> GetCompiledScheduleSettings(string assetPairId,
            DateTime currentDateTime, TimeSpan scheduleCutOff);

        void CacheWarmUp();
        
        Task UpdateSettingsAsync();
    }
}