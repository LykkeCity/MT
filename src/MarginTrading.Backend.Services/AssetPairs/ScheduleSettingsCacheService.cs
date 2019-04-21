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
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Services;
using MarginTrading.SettingsService.Contracts;
using MarginTrading.SettingsService.Contracts.Scheduling;
using MoreLinq;
using ScheduleSettingsContract = MarginTrading.SettingsService.Contracts.Scheduling.ScheduleSettingsContract;

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

        private Dictionary<string, List<ScheduleSettings>> _rawScheduleSettingsCache =
            new Dictionary<string, List<ScheduleSettings>>();

        private Dictionary<string, List<CompiledScheduleTimeInterval>> _compiledScheduleTimelineCache =
            new Dictionary<string, List<CompiledScheduleTimeInterval>>();

        private List<ScheduleSettings> _rawPlatformSchedule = new List<ScheduleSettings>();
        private List<CompiledScheduleTimeInterval> _compiledPlatformSchedule = new List<CompiledScheduleTimeInterval>();

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
            await UpdateSettingsAsync();
            await UpdatePlatformSettingsAsync();
        }

        public async Task UpdateSettingsAsync()
        {
            var newScheduleContracts = (await _scheduleSettingsApi.StateList(_assetPairsCache.GetAllIds().ToArray()))
                .Where(x => x.ScheduleSettings.Any()).ToList();
            var invalidSchedules = InvalidSchedules(newScheduleContracts);

            _readerWriterLockSlim.EnterWriteLock();

            try
            {
                var newRawScheduleSettings = newScheduleContracts.ToDictionary(x => x.AssetPairId,
                    x => x.ScheduleSettings.Except(invalidSchedules.TryGetValue(x.AssetPairId, out var invalid)
                            ? invalid
                            : new List<CompiledScheduleSettingsContract>())
                        .Select(ScheduleSettings.Create).ToList());

                _rawScheduleSettingsCache
                    .Where(x => TradingScheduleChanged(x.Key, _rawScheduleSettingsCache, newRawScheduleSettings))
                    .Select(x => x.Key)
                    .ForEach(key => _compiledScheduleTimelineCache.Remove(key));

                _rawScheduleSettingsCache = newRawScheduleSettings;

                _lastCacheRecalculationTime = _dateService.Now();
            }
            catch (Exception exception)
            {
                await _log.WriteErrorAsync(nameof(ScheduleSettingsCacheService), nameof(UpdateSettingsAsync),
                    exception);
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
                CacheWarmUp();
            }

            if (invalidSchedules.Any())
            {
                await _log.WriteWarningAsync(nameof(ScheduleSettingsCacheService), nameof(UpdateSettingsAsync),
                    $"Some of CompiledScheduleSettingsContracts were invalid, so they were skipped. The first one: {invalidSchedules.First().ToJson()}");
            }
        }

        public async Task UpdatePlatformSettingsAsync()
        {
            var platformSettingsRaw = (await _scheduleSettingsApi.List(_overnightMarginSettings.ScheduleMarketId))
                .ToList();
            var invalidSchedules = InvalidSchedules(platformSettingsRaw); 
            var platformSettings = platformSettingsRaw.Except(invalidSchedules)
                .Select(ScheduleSettings.Create).ToList();

            _readerWriterLockSlim.EnterWriteLock();

            try
            {
                _rawPlatformSchedule = platformSettings;
            
                PlatformCacheWarmUpUnsafe();
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
            
            if (invalidSchedules.Any())
            {
                await _log.WriteWarningAsync(nameof(ScheduleSettingsCacheService), nameof(UpdatePlatformSettingsAsync),
                    $"Some of ScheduleSettingsContracts were invalid, so they were skipped. The first one: {invalidSchedules.First().ToJson()}");
            }
        }

        /// <inheritdoc cref="IScheduleSettingsCacheService"/>
        public List<CompiledScheduleTimeInterval> GetPlatformTradingSchedule()
        {
            _readerWriterLockSlim.EnterReadLock();

            try
            {
                return _compiledPlatformSchedule.ToList();
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        public bool GetPlatformTradingEnabled()
        {
            var platformSchedule = GetPlatformTradingSchedule();

            return GetTradingEnabled(platformSchedule);
        }

        public bool AssetPairTradingEnabled(string assetPairId, TimeSpan scheduleCutOff)
        {
            var schedule = GetCompiledScheduleSettings(assetPairId, _dateService.Now(), scheduleCutOff);

            return GetTradingEnabled(schedule);
        }

        private bool GetTradingEnabled(IEnumerable<CompiledScheduleTimeInterval> timeIntervals)
        {
            var currentDateTime = _dateService.Now();
            
            var intersecting = timeIntervals.Where(x => x.Start <= currentDateTime && currentDateTime < x.End);

            return intersecting
                       .OrderByDescending(x => x.Schedule.Rank)
                       .Select(x => x.Schedule)
                       .FirstOrDefault()?
                       .IsTradeEnabled ?? true;
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

        public Dictionary<string, List<CompiledScheduleTimeInterval>> GetCompiledScheduleSettings(
            DateTime currentDateTime, TimeSpan scheduleCutOff)
        {
            _readerWriterLockSlim.EnterReadLock();

            try
            {
                EnsureCacheValidUnsafe(currentDateTime);
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }

            if (!_compiledScheduleTimelineCache.Any())
            {
                CacheWarmUp();
            }

            return _compiledScheduleTimelineCache;
        }

        public List<CompiledScheduleTimeInterval> GetCompiledScheduleSettings(string assetPairId,
            DateTime currentDateTime, TimeSpan scheduleCutOff)
        {
            _readerWriterLockSlim.EnterUpgradeableReadLock();

            EnsureCacheValidUnsafe(currentDateTime);

            try
            {
                if (string.IsNullOrEmpty(assetPairId))
                {
                    return new List<CompiledScheduleTimeInterval>();
                }

                if (!_compiledScheduleTimelineCache.ContainsKey(assetPairId))
                {
                    RecompileScheduleTimelineCacheUnsafe(assetPairId, currentDateTime, scheduleCutOff);
                }

                return _compiledScheduleTimelineCache.TryGetValue(assetPairId, out var timeline)
                    ? timeline
                    : new List<CompiledScheduleTimeInterval>();
            }
            finally
            {
                _readerWriterLockSlim.ExitUpgradeableReadLock();
            }
        }

        public void CacheWarmUp()
        {
            var currentDateTime = _dateService.Now();
            var assetPairIds = _assetPairsCache.GetAllIds();

            _readerWriterLockSlim.EnterUpgradeableReadLock();

            try
            {
                foreach (var assetPairId in assetPairIds)
                {
                    if (!_compiledScheduleTimelineCache.ContainsKey(assetPairId))
                    {
//todo Zero timespan is ok for market orders, but if pending cut off should be applied, we will need one more cache for them..
                        RecompileScheduleTimelineCacheUnsafe(assetPairId, currentDateTime, TimeSpan.Zero);
                    }
                }
            }
            finally
            {
                _readerWriterLockSlim.ExitUpgradeableReadLock();
            }
        }

        public void PlatformCacheWarmUp()
        {
            _readerWriterLockSlim.EnterWriteLock();

            try
            {
                PlatformCacheWarmUpUnsafe();
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();   
            }
        }

        private void PlatformCacheWarmUpUnsafe()
        {
            _compiledPlatformSchedule = CompileSchedule(_rawPlatformSchedule, _dateService.Now(), TimeSpan.Zero);
        }

        /// <summary>
        /// It invalidates the cache after 00:00:00.000 each day on request
        /// </summary>
        /// <param name="currentDateTime"></param>
        private void EnsureCacheValidUnsafe(DateTime currentDateTime)
        {
            //it must be safe to take _lastCacheRecalculationTime without a lock, because of upper UpgradeableReadLock
            if (_lastCacheRecalculationTime.Date.Subtract(currentDateTime.Date) < TimeSpan.FromDays(1))
            {
                return;
            }

            _readerWriterLockSlim.EnterWriteLock();

            try
            {
                _compiledScheduleTimelineCache =
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
            var scheduleSettings = _rawScheduleSettingsCache.TryGetValue(assetPairId, out var settings)
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
                _compiledScheduleTimelineCache[assetPairId] = resultingTimeIntervals;

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
                        new CompiledScheduleTimeInterval(sch, currentStart.AddDays(-7), currentEnd.AddDays(-7))
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
                    var start = currentDateTime.Date.Add(sch.Start.Time);
                    var end = currentDateTime.Date.Add(sch.End.Time);
                    if (end < start)
                    {
                        end = end.AddDays(1);
                    }

                    return new[]
                    {
                        new CompiledScheduleTimeInterval(sch, start, end),
                        new CompiledScheduleTimeInterval(sch, start.AddDays(-1), end.AddDays(-1))
                    };
                })
                : new List<CompiledScheduleTimeInterval>();

            return weekly.Concat(single).Concat(daily).ToList();
        }

        private static DateTime CurrentWeekday(DateTime start, DayOfWeek day)
        {
            return start.Date.AddDays((int) day - (int) start.DayOfWeek);
        }
        
        private static Dictionary<string, List<CompiledScheduleSettingsContract>> InvalidSchedules(
            IEnumerable<CompiledScheduleContract> scheduleContracts)
        {
            var invalidSchedules = new Dictionary<string, List<CompiledScheduleSettingsContract>>();
            foreach (var scheduleContract in scheduleContracts)
            {
                var scheduleSettings = new List<CompiledScheduleSettingsContract>();
                foreach (var scheduleSetting in scheduleContract.ScheduleSettings)
                {
                    try
                    {
                        ScheduleConstraintContract.Validate(scheduleSetting);
                    }
                    catch
                    {
                        scheduleSettings.Add(scheduleSetting);
                    }
                }

                if (scheduleSettings.Any())
                {
                    invalidSchedules.Add(scheduleContract.AssetPairId, scheduleSettings);
                }
            }

            return invalidSchedules;
        }
        
        private static List<ScheduleSettingsContract> InvalidSchedules(
            IEnumerable<ScheduleSettingsContract> scheduleContracts)
        {
            var scheduleSettings = new List<ScheduleSettingsContract>();
            foreach (var scheduleSetting in scheduleContracts)
            {
                try
                {
                    ScheduleConstraintContract.Validate(scheduleSetting);
                }
                catch
                {
                    scheduleSettings.Add(scheduleSetting);
                }
            }

            return scheduleSettings;
        }
    }
}