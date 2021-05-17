// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.DayOffSettings;

namespace MarginTrading.Backend.Services.AssetPairs
{
    public interface IScheduleSettingsCacheService
    {
        [Obsolete]
        Dictionary<string, List<CompiledScheduleTimeInterval>> GetCompiledAssetPairScheduleSettings();

        void CacheWarmUpIncludingValidation();

        void MarketsCacheWarmUp();

        Task UpdateAllSettingsAsync(bool forcePublishMarketStateChanged = false);
        
        Task UpdateScheduleSettingsAsync();

        /// <summary>
        /// Get current and next day time intervals of the platform disablement hours.
        /// </summary>
        List<CompiledScheduleTimeInterval> GetPlatformTradingSchedule();

        Dictionary<string, List<CompiledScheduleTimeInterval>> GetMarketsTradingSchedule();

        Dictionary<string, MarketState> GetMarketState();

        void HandleMarketStateChanges(DateTime currentTime);
        
        bool TryGetPlatformCurrentDisabledInterval(out CompiledScheduleTimeInterval disabledInterval);

        bool AssetPairTradingEnabled(string assetPairId, TimeSpan scheduleCutOff);

        /// <inheritdoc cref="IScheduleSettingsCacheService"/>
        List<CompiledScheduleTimeInterval> GetMarketTradingScheduleByAssetPair(string assetPairId);
    }
}