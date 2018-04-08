using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Autofac;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Services.AssetPairs
{
    internal class DayOffSettingsService : IDayOffSettingsService, IStartable
    {
        [CanBeNull] private DayOffSettingsRoot _cache;
        private static readonly object _updateLock = new object();

        private readonly IDayOffSettingsRepository _dayOffSettingsRepository;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly ICachedCalculation<ImmutableDictionary<string, ImmutableArray<DayOffExclusion>>> _exclusionsByAssetPairId;

        public DayOffSettingsService(IDayOffSettingsRepository dayOffSettingsRepository, IAssetPairsCache assetPairsCache)
        {
            _dayOffSettingsRepository = dayOffSettingsRepository;
            _assetPairsCache = assetPairsCache;
            _exclusionsByAssetPairId = GetExclusionsByAssetPairIdCache();
        }

        public ImmutableDictionary<Guid, DayOffExclusion> GetExclusions()
        {
            return GetRoot().Exclusions;
        }

        public ImmutableDictionary<string, ImmutableArray<DayOffExclusion>> GetCompiledExclusions()
        {
            return _exclusionsByAssetPairId.Get();
        }

        public ScheduleSettings GetScheduleSettings()
        {
            return GetRoot().ScheduleSettings;
        }

        public ScheduleSettings SetScheduleSettings(ScheduleSettings scheduleSettings)
        {
            Change(root => new DayOffSettingsRoot(root.Exclusions, scheduleSettings));
            return scheduleSettings;
        }

        public IReadOnlyList<DayOffExclusion> GetExclusions(string assetPairId)
        {
            return _exclusionsByAssetPairId.Get().GetValueOrDefault(assetPairId, ImmutableArray<DayOffExclusion>.Empty);
        }

        public void DeleteExclusion(Guid id)
        {
            id.RequiredNotEqualsTo(default, nameof(id));
            Change(root =>
            {
                root.Exclusions.ContainsKey(id).RequiredEqualsTo(true, "oldExclusion",
                    "Trying to delete non-existent exclusion with id " + id);
                return new DayOffSettingsRoot(root.Exclusions.Remove(id), root.ScheduleSettings);
            });
        }

        private DayOffSettingsRoot GetRoot()
        {
            return _cache.RequiredNotNull("_cache != null");
        }

        public DayOffExclusion GetExclusion(Guid id)
        {
            id.RequiredNotEqualsTo(default, nameof(id));
            return GetExclusions().GetValueOrDefault(id);
        }

        public DayOffExclusion CreateExclusion([NotNull] DayOffExclusion exclusion)
        {
            if (exclusion == null) throw new ArgumentNullException(nameof(exclusion));
            exclusion.Id.RequiredNotEqualsTo(default, nameof(exclusion.Id));
            Change(root =>
            {
                root.Exclusions.ContainsKey(exclusion.Id).RequiredEqualsTo(false, "oldExclusion",
                    "Trying to add already existing exclusion with id " + exclusion.Id);
                return SetExclusion(root, exclusion);
            });
            return exclusion;
        }

        public DayOffExclusion UpdateExclusion([NotNull] DayOffExclusion exclusion)
        {
            if (exclusion == null) throw new ArgumentNullException(nameof(exclusion));
            exclusion.Id.RequiredNotEqualsTo(default, nameof(exclusion.Id));
            Change(root =>
            {
                root.Exclusions.ContainsKey(exclusion.Id).RequiredEqualsTo(true, "oldExclusion",
                    "Trying to update non-existent exclusion with id " + exclusion.Id);
                return SetExclusion(root, exclusion);
            });
            
            return exclusion;
        }

        public void Start()
        {
            _cache = _dayOffSettingsRepository.Read()
                     ?? new DayOffSettingsRoot(ImmutableDictionary<Guid, DayOffExclusion>.Empty,
                         new ScheduleSettings(
                             dayOffStartDay: DayOfWeek.Friday,
                             dayOffStartTime: new TimeSpan(20, 55, 0),
                             dayOffEndDay: DayOfWeek.Sunday,
                             dayOffEndTime: new TimeSpan(22, 05, 0),
                             assetPairsWithoutDayOff: new[] {"BTCUSD"}.ToHashSet(),
                             pendingOrdersCutOff: new TimeSpan(0, 55, 0)));
        }

        private static DayOffSettingsRoot SetExclusion(DayOffSettingsRoot old, DayOffExclusion exclusion)
        {
            return new DayOffSettingsRoot(old.Exclusions.SetItem(exclusion.Id, exclusion), old.ScheduleSettings);
        }

        private void Change(Func<DayOffSettingsRoot, DayOffSettingsRoot> changeFunc)
        {
            lock (_updateLock)
            {
                var oldSettings = _cache;
                var settings = changeFunc(oldSettings);
                _dayOffSettingsRepository.Write(settings);
                _cache = settings;
            }
        }

        private ICachedCalculation<ImmutableDictionary<string, ImmutableArray<DayOffExclusion>>> GetExclusionsByAssetPairIdCache()
        {
            return Calculate.Cached(
                () => (Exclusions: GetExclusions(), Pairs: _assetPairsCache.GetAllIds()),
                (o, n) => ReferenceEquals(o.Exclusions, n.Exclusions) && o.Pairs.SetEquals(n.Pairs),
                t => t.Pairs.ToImmutableDictionary(p => p,
                    p => t.Exclusions.Values.Where(v => Regex.IsMatch(p, v.AssetPairRegex, RegexOptions.IgnoreCase)).ToImmutableArray()));
        }
    }
}