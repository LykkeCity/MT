using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.DayOffSettings;

namespace MarginTrading.Backend.Services.AssetPairs
{
    public interface IDayOffSettingsService
    {
        ImmutableDictionary<Guid, DayOffExclusion> GetExclusions();
        [CanBeNull] DayOffExclusion GetExclusion(Guid id);
        DayOffExclusion CreateExclusion(DayOffExclusion exclusion);
        DayOffExclusion UpdateExclusion(DayOffExclusion exclusion);
        ScheduleSettings GetScheduleSettings();
        ScheduleSettings SetScheduleSettings(ScheduleSettings scheduleSettings);
        IReadOnlyList<DayOffExclusion> GetExclusions(string assetPairId);
        void DeleteExclusion(Guid id);
        ImmutableDictionary<string, ImmutableArray<DayOffExclusion>> GetCompiledExclusions();
    }
}