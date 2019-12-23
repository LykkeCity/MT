// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
        Dictionary<string, List<CompiledScheduleTimeInterval>> GetCompiledAssetPairScheduleSettings();
        
        /// <summary>
        /// Get compiled schedule timeline from cache, recalculate it if needed.
        /// </summary>
        List<CompiledScheduleTimeInterval> GetCompiledAssetPairScheduleSettings(string assetPairId,
            DateTime currentDateTime, TimeSpan scheduleCutOff);

        void CacheWarmUp(params string[] assetPairIds);

        void CacheWarmUpIncludingValidation();

        void MarketsCacheWarmUp();

        Task UpdateAllSettingsAsync();
        
        Task UpdateScheduleSettingsAsync();

        Task UpdateMarketsScheduleSettingsAsync();

        /// <summary>
        /// Get current and next day time intervals of the platform disablement hours.
        /// </summary>
        List<CompiledScheduleTimeInterval> GetPlatformTradingSchedule();

        Dictionary<string, List<CompiledScheduleTimeInterval>> GetMarketsTradingSchedule();

        Dictionary<string, MarketState> GetMarketState();

        void HandleMarketStateChanges(DateTime currentTime);
        
        bool TryGetPlatformCurrentDisabledInterval(out CompiledScheduleTimeInterval disabledInterval);

        bool AssetPairTradingEnabled(string assetPairId, TimeSpan scheduleCutOff);
    }
}