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
        List<ScheduleSettings> GetScheduleSettings(string assetPairId);
        Task UpdateSettingsAsync();
    }
}