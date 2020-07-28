// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.TradingSchedule;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;
using MarginTrading.AssetService.Contracts;
using MarginTrading.AssetService.Contracts.Scheduling;
using MoreLinq;
using ScheduleSettingsContract = MarginTrading.AssetService.Contracts.Scheduling.ScheduleSettingsContract;

namespace MarginTrading.Backend.Services.AssetPairs
{
    public class ScheduleSettingsCacheService : IScheduleSettingsCacheService
    {
        private readonly ICqrsSender _cqrsSender;
        private readonly IScheduleSettingsApi _scheduleSettingsApi;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IDateService _dateService;
        private readonly ILog _log;
        private readonly OvernightMarginSettings _overnightMarginSettings;

        private Dictionary<string, List<ScheduleSettings>> _rawAssetPairScheduleCache =
            new Dictionary<string, List<ScheduleSettings>>();
        private Dictionary<string, List<CompiledScheduleTimeInterval>> _compiledAssetPairScheduleCache =
            new Dictionary<string, List<CompiledScheduleTimeInterval>>();

        private Dictionary<string, List<ScheduleSettings>> _rawMarketScheduleCache =
            new Dictionary<string, List<ScheduleSettings>>();
        private Dictionary<string, List<CompiledScheduleTimeInterval>> _compiledMarketScheduleCache =
            new Dictionary<string, List<CompiledScheduleTimeInterval>>();

        private readonly Dictionary<string, MarketState> _marketStates = new Dictionary<string, MarketState>();

        private DateTime _lastCacheRecalculationTime = DateTime.MinValue;

        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();

        public ScheduleSettingsCacheService(
            ICqrsSender cqrsSender,
            IScheduleSettingsApi scheduleSettingsApi,
            IAssetPairsCache assetPairsCache,
            IDateService dateService,
            ILog log,
            OvernightMarginSettings overnightMarginSettings)
        {
            _cqrsSender = cqrsSender;
            _scheduleSettingsApi = scheduleSettingsApi;
            _assetPairsCache = assetPairsCache;
            _dateService = dateService;
            _log = log;
            _overnightMarginSettings = overnightMarginSettings;
        }

        public async Task UpdateAllSettingsAsync()
        {
            await UpdateScheduleSettingsAsync();
            await UpdateMarketsScheduleSettingsAsync();
        }

        public async Task UpdateScheduleSettingsAsync()
        {
            var newScheduleContracts = (await _scheduleSettingsApi.StateList(null))
                .Where(x => x.ScheduleSettings.Any()).ToList();
            var invalidSchedules = newScheduleContracts.InvalidSchedules();

            var assertPairIdsToWarmUp = new List<string>();

            _readerWriterLockSlim.EnterWriteLock();

            try
            {
                var newRawScheduleSettings = newScheduleContracts.ToDictionary(x => x.AssetPairId,
                    x => x.ScheduleSettings.Except(invalidSchedules.TryGetValue(x.AssetPairId, out var invalid)
                            ? invalid
                            : new List<CompiledScheduleSettingsContract>())
                        .Select(ScheduleSettings.Create).ToList());

                _rawAssetPairScheduleCache
                    .Where(x => TradingScheduleChanged(x.Key, _rawAssetPairScheduleCache, newRawScheduleSettings))
                    .Select(x => x.Key)
                    .ForEach(key =>
                    {
                        _compiledAssetPairScheduleCache.Remove(key);
                        assertPairIdsToWarmUp.Add(key);
                    });

                _rawAssetPairScheduleCache = newRawScheduleSettings;
            }
            catch (Exception exception)
            {
                await _log.WriteErrorAsync(nameof(ScheduleSettingsCacheService), nameof(UpdateScheduleSettingsAsync),
                    exception);
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
                CacheWarmUp(assertPairIdsToWarmUp.ToArray());
            }

            if (invalidSchedules.Any())
            {
                await _log.WriteWarningAsync(nameof(ScheduleSettingsCacheService), nameof(UpdateScheduleSettingsAsync),
                    $"Some of CompiledScheduleSettingsContracts were invalid, so they were skipped. The first one: {invalidSchedules.First().ToJson()}");
            }
        }

        public async Task UpdateMarketsScheduleSettingsAsync()
        {
            var marketsScheduleSettingsRaw = (await _scheduleSettingsApi.List())
                .Where(x => !string.IsNullOrWhiteSpace(x.MarketId))
                .ToList();
            var invalidSchedules = marketsScheduleSettingsRaw.InvalidSchedules();

            _readerWriterLockSlim.EnterWriteLock();

            try
            {
                var platformScheduleSettings = marketsScheduleSettingsRaw
                    .Where(x => x.MarketId == _overnightMarginSettings.ScheduleMarketId).ToList();
                
                var newMarketsScheduleSettings = marketsScheduleSettingsRaw
                    .Except(invalidSchedules)
                    .GroupBy(x => x.MarketId)
                    .ToDictionary(x => x.Key, x => x.ConcatWithPlatform(platformScheduleSettings, x.Key)
                        .Select(ScheduleSettings.Create)
                        .ToList());

                _rawMarketScheduleCache = newMarketsScheduleSettings;

                var now = MarketsCacheWarmUpUnsafe();

                HandleMarketStateChangesUnsafe(now, newMarketsScheduleSettings.Keys.ToArray());
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }

            if (invalidSchedules.Any())
            {
                await _log.WriteWarningAsync(nameof(ScheduleSettingsCacheService), nameof(UpdateMarketsScheduleSettingsAsync),
                    $"{invalidSchedules.Count} of ScheduleSettingsContracts were invalid, so they were skipped: {invalidSchedules.ToJson()}");
            }
        }

        public void HandleMarketStateChanges(DateTime currentTime)
        {
            _readerWriterLockSlim.EnterWriteLock();
            
            try
            {
                HandleMarketStateChangesUnsafe(currentTime);
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        private void HandleMarketStateChangesUnsafe(DateTime currentTime, string[] marketIds = null)
        {
            foreach (var (marketId, scheduleSettings) in _compiledMarketScheduleCache
                // ReSharper disable once AssignNullToNotNullAttribute
                .Where(x => marketIds.IsNullOrEmpty() || marketIds.Contains(x.Key)))
            {
                var newState = scheduleSettings.GetMarketState(marketId, currentTime);
                if (!_marketStates.TryGetValue(marketId, out var oldState) || oldState.IsEnabled != newState.IsEnabled)
                {
                    _cqrsSender.PublishEvent(new MarketStateChangedEvent
                    {
                        Id = marketId,
                        IsEnabled = newState.IsEnabled,
                        EventTimestamp = _dateService.Now(),
                    });
                }

                _marketStates[marketId] = newState;
            }
        }

        /// <inheritdoc cref="IScheduleSettingsCacheService"/>
        public List<CompiledScheduleTimeInterval> GetPlatformTradingSchedule()
        {
            _readerWriterLockSlim.EnterReadLock();

            try
            {
                return _compiledMarketScheduleCache[_overnightMarginSettings.ScheduleMarketId].ToList();
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        /// <inheritdoc cref="IScheduleSettingsCacheService"/>
        public Dictionary<string, List<CompiledScheduleTimeInterval>> GetMarketsTradingSchedule()
        {
            _readerWriterLockSlim.EnterReadLock();

            try
            {
                return _compiledMarketScheduleCache.ToDictionary();
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        public bool TryGetPlatformCurrentDisabledInterval(out CompiledScheduleTimeInterval disabledInterval)
        {
            var platformSchedule = GetPlatformTradingSchedule();

            return !GetTradingEnabled(platformSchedule, out disabledInterval);
        }

        public bool AssetPairTradingEnabled(string assetPairId, TimeSpan scheduleCutOff)
        {
            var schedule = GetCompiledAssetPairScheduleSettings(assetPairId, _dateService.Now(), scheduleCutOff);

            return GetTradingEnabled(schedule, out _);
        }

        private bool GetTradingEnabled(IEnumerable<CompiledScheduleTimeInterval> timeIntervals,
            out CompiledScheduleTimeInterval selectedInterval)
        {
            var currentDateTime = _dateService.Now();

            var intersecting = timeIntervals.Where(x => x.Start <= currentDateTime && currentDateTime < x.End);

            selectedInterval = intersecting
                       .OrderByDescending(x => x.Schedule.Rank)
                       .FirstOrDefault();

            return selectedInterval?.Schedule.IsTradeEnabled ?? true;
        }

        private static bool TradingScheduleChanged(string key,
            Dictionary<string, List<ScheduleSettings>> oldRawScheduleSettingsCache,
            Dictionary<string, List<ScheduleSettings>> newRawScheduleSettingsCache)
        {
            if (!oldRawScheduleSettingsCache.TryGetValue(key, out var oldScheduleSettings)
                || !newRawScheduleSettingsCache.TryGetValue(key, out var newRawScheduleSettings)
                || oldScheduleSettings.Count != newRawScheduleSettings.Count)
            {
                return true;
            }

            foreach (var oldScheduleSetting in oldScheduleSettings)
            {
                var newScheduleSetting = newRawScheduleSettings.FirstOrDefault(x => x.Id == oldScheduleSetting.Id);

                if (newScheduleSetting == null
                    || newScheduleSetting.Rank != oldScheduleSetting.Rank
                    || newScheduleSetting.IsTradeEnabled != oldScheduleSetting.IsTradeEnabled
                    || newScheduleSetting.PendingOrdersCutOff != oldScheduleSetting.PendingOrdersCutOff
                    || !newScheduleSetting.Start.Equals(oldScheduleSetting.Start)
                    || !newScheduleSetting.End.Equals(oldScheduleSetting.End))
                {
                    return true;
                }
            }

            return false;
        }

        public Dictionary<string, List<CompiledScheduleTimeInterval>> GetCompiledAssetPairScheduleSettings()
        {
            _readerWriterLockSlim.EnterUpgradeableReadLock();

            try
            {
                CacheWarmUpIncludingValidationUnsafe();

                return _compiledAssetPairScheduleCache;
            }
            finally
            {
                _readerWriterLockSlim.ExitUpgradeableReadLock();
            }
        }

        public Dictionary<string, MarketState> GetMarketState()
        {
            _readerWriterLockSlim.EnterReadLock();

            try
            {
                return _marketStates.ToDictionary();
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        public List<CompiledScheduleTimeInterval> GetCompiledAssetPairScheduleSettings(string assetPairId,
            DateTime currentDateTime, TimeSpan scheduleCutOff)
        {
            if (string.IsNullOrEmpty(assetPairId))
            {
                return new List<CompiledScheduleTimeInterval>();
            }

            _readerWriterLockSlim.EnterUpgradeableReadLock();

            EnsureCacheValidUnsafe(currentDateTime);

            try
            {
                if (!_compiledAssetPairScheduleCache.ContainsKey(assetPairId))
                {
                    RecompileScheduleTimelineCacheUnsafe(assetPairId, currentDateTime, scheduleCutOff);
                }

                return _compiledAssetPairScheduleCache.TryGetValue(assetPairId, out var timeline)
                    ? timeline
                    : new List<CompiledScheduleTimeInterval>();
            }
            finally
            {
                _readerWriterLockSlim.ExitUpgradeableReadLock();
            }
        }

        public void CacheWarmUpIncludingValidation()
        {
            _readerWriterLockSlim.EnterUpgradeableReadLock();

            try
            {
                CacheWarmUpIncludingValidationUnsafe();
            }
            finally
            {
                _readerWriterLockSlim.ExitUpgradeableReadLock();
            }
        }

        private void CacheWarmUpIncludingValidationUnsafe()
        {
            EnsureCacheValidUnsafe(_dateService.Now());

            if (!_compiledAssetPairScheduleCache.Any())
            {
                CacheWarmUpUnsafe();
            }
        }

        public void CacheWarmUp(params string[] assetPairIds)
        {
            _log.WriteInfoAsync(nameof(ScheduleSettingsCacheService), nameof(CacheWarmUp),
                "Started asset pairs schedule cache update");

            _readerWriterLockSlim.EnterUpgradeableReadLock();

            try
            {
                CacheWarmUpUnsafe(assetPairIds);
            }
            finally
            {
                _readerWriterLockSlim.ExitUpgradeableReadLock();
            }

            _log.WriteInfoAsync(nameof(ScheduleSettingsCacheService), nameof(CacheWarmUp),
                "Finished asset pairs schedule cache update");
        }

        private void CacheWarmUpUnsafe(params string[] assetPairIds)
        {
            var currentDateTime = _dateService.Now();
            var assetPairIdsToWarmUp = assetPairIds.Any()
                ? assetPairIds.ToArray()
                : _assetPairsCache.GetAllIds().ToArray();

            foreach (var assetPairId in assetPairIdsToWarmUp)
            {
                if (!_compiledAssetPairScheduleCache.ContainsKey(assetPairId))
                {
                    //todo Zero timespan is ok for market orders, but if pending cut off should be applied, we will need one more cache for them..
                    RecompileScheduleTimelineCacheUnsafe(assetPairId, currentDateTime, TimeSpan.Zero);
                }
            }
        }

        public void MarketsCacheWarmUp()
        {
            _log.WriteInfoAsync(nameof(ScheduleSettingsCacheService), nameof(MarketsCacheWarmUp),
                "Started market schedule cache update");

            _readerWriterLockSlim.EnterWriteLock();

            try
            {
                MarketsCacheWarmUpUnsafe();
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }

            _log.WriteInfoAsync(nameof(ScheduleSettingsCacheService), nameof(MarketsCacheWarmUp),
                "Finished market schedule cache update");
        }

        private DateTime MarketsCacheWarmUpUnsafe()
        {
            var now = _dateService.Now();
            
            _compiledMarketScheduleCache = _rawMarketScheduleCache
                .ToDictionary(x => x.Key, x => CompileSchedule(x.Value, now, TimeSpan.Zero));
            HandleMarketStateChangesUnsafe(now, _rawMarketScheduleCache.Keys.ToArray());

            return now;
        }

        /// <summary>
        /// It invalidates the cache after 00:00:00.000 each day on request
        /// </summary>
        /// <param name="currentDateTime"></param>
        private void EnsureCacheValidUnsafe(DateTime currentDateTime)
        {
            //it must be safe to take _lastCacheRecalculationTime without a lock, because of upper UpgradeableReadLock
            if (currentDateTime.Date.Subtract(_lastCacheRecalculationTime.Date) < TimeSpan.FromDays(1))
            {
                return;
            }

            _readerWriterLockSlim.EnterWriteLock();

            try
            {
                _compiledAssetPairScheduleCache =
                    new Dictionary<string, List<CompiledScheduleTimeInterval>>();
                _lastCacheRecalculationTime = currentDateTime;
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        private void RecompileScheduleTimelineCacheUnsafe(string assetPairId, DateTime currentDateTime,
            TimeSpan scheduleCutOff)
        {
            var scheduleSettings = _rawAssetPairScheduleCache.TryGetValue(assetPairId, out var settings)
                ? settings
                : new List<ScheduleSettings>();

            if (!scheduleSettings.Any())
            {
                return;
            }

            var resultingTimeIntervals = CompileSchedule(scheduleSettings, currentDateTime, scheduleCutOff);

            _readerWriterLockSlim.EnterWriteLock();
            try
            {
                _compiledAssetPairScheduleCache[assetPairId] = resultingTimeIntervals;

                _cqrsSender.PublishEvent(new CompiledScheduleChangedEvent
                {
                    AssetPairId = assetPairId,
                    EventTimestamp = _dateService.Now(),
                    TimeIntervals = resultingTimeIntervals.Select(x => x.ToRabbitMqContract()).ToList(),
                });
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        private static List<CompiledScheduleTimeInterval> CompileSchedule(
            IEnumerable<ScheduleSettings> scheduleSettings, DateTime currentDateTime, TimeSpan scheduleCutOff)
        {
            var scheduleSettingsByType = scheduleSettings
                .GroupBy(x => x.Start.GetConstraintType())
                .ToDictionary(x => x.Key, value => value);

            //handle weekly
            var weekly = scheduleSettingsByType.TryGetValue(ScheduleConstraintType.Weekly, out var weeklySchedule)
                ? weeklySchedule.SelectMany(sch =>
                {
                    var currentStart = CurrentWeekday(currentDateTime, sch.Start.DayOfWeek.Value)
                        .Add(sch.Start.Time.Subtract(scheduleCutOff));
                    var currentEnd = CurrentWeekday(currentDateTime, sch.End.DayOfWeek.Value)
                        .Add(sch.End.Time.Add(scheduleCutOff));
                    if (currentEnd < currentStart)
                    {
                        currentEnd = currentEnd.AddDays(7);
                    }

                    return new[]
                    {
                        new CompiledScheduleTimeInterval(sch, currentStart, currentEnd),
                        new CompiledScheduleTimeInterval(sch, currentStart.AddDays(-7), currentEnd.AddDays(-7)),
                        new CompiledScheduleTimeInterval(sch, currentStart.AddDays(7), currentEnd.AddDays(7))
                    };
                })
                : new List<CompiledScheduleTimeInterval>();

            //handle single
            var single = scheduleSettingsByType.TryGetValue(ScheduleConstraintType.Single, out var singleSchedule)
                ? singleSchedule.Select(sch => new CompiledScheduleTimeInterval(sch,
                    sch.Start.Date.Value.Add(sch.Start.Time.Subtract(scheduleCutOff)),
                    sch.End.Date.Value.Add(sch.End.Time.Add(scheduleCutOff))))
                : new List<CompiledScheduleTimeInterval>();

            //handle daily
            var daily = scheduleSettingsByType.TryGetValue(ScheduleConstraintType.Daily, out var dailySchedule)
                ? dailySchedule.SelectMany(sch =>
                {
                    var start = currentDateTime.Date.Add(sch.Start.Time.Subtract(scheduleCutOff));
                    var end = currentDateTime.Date.Add(sch.End.Time.Add(scheduleCutOff));
                    if (end < start)
                    {
                        end = end.AddDays(1);
                    }

                    return new[]
                    {
                        new CompiledScheduleTimeInterval(sch, start, end),
                        new CompiledScheduleTimeInterval(sch, start.AddDays(-1), end.AddDays(-1)),
                        new CompiledScheduleTimeInterval(sch, start.AddDays(1), end.AddDays(1))
                    };
                })
                : new List<CompiledScheduleTimeInterval>();

            return weekly.Concat(single).Concat(daily).ToList();
        }

        private static DateTime CurrentWeekday(DateTime start, DayOfWeek day)
        {
            return start.Date.AddDays((int)day - (int)start.DayOfWeek);
        }
    }
}