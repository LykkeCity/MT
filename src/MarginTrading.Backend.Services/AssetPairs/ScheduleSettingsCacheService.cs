using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.TradingSchedule;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services;
using MarginTrading.SettingsService.Contracts;
using MarginTrading.SettingsService.Contracts.Scheduling;

namespace MarginTrading.Backend.Services.AssetPairs
{
    public class ScheduleSettingsCacheService : IScheduleSettingsCacheService
    {
        private readonly ICqrsSender _cqrsSender;
        private readonly IScheduleSettingsApi _scheduleSettingsApi;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IDateService _dateService;
        private readonly ILog _log;
        private readonly CqrsContextNamesSettings _cqrsContextNamesSettings;

        private Dictionary<string, List<ScheduleSettings>> _rawScheduleSettingsCache =
            new Dictionary<string, List<ScheduleSettings>>();
        private Dictionary<string, List<CompiledScheduleTimeInterval>> _compiledScheduleTimelineCache = 
            new Dictionary<string, List<CompiledScheduleTimeInterval>>();
        private DateTime _lastCacheRecalculationTime = DateTime.MinValue;

        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();

        public ScheduleSettingsCacheService(
            ICqrsSender cqrsSender,
            IScheduleSettingsApi scheduleSettingsApi,
            IAssetPairsCache assetPairsCache,
            IDateService dateService,
            ILog log,
            CqrsContextNamesSettings cqrsContextNamesSettings)
        {
            _cqrsSender = cqrsSender;
            _scheduleSettingsApi = scheduleSettingsApi;
            _assetPairsCache = assetPairsCache;
            _dateService = dateService;
            _log = log;
            _cqrsContextNamesSettings = cqrsContextNamesSettings;
        }

        public async Task UpdateSettingsAsync()
        {
            var newScheduleContracts = (await _scheduleSettingsApi.StateList(_assetPairsCache.GetAllIds().ToArray()))
                .Where(x => x.ScheduleSettings.Any()).ToList();
            var invalidSchedules = GetInvalidSchedules(newScheduleContracts);
            
            _readerWriterLockSlim.EnterWriteLock();

            try
            {
                _rawScheduleSettingsCache = newScheduleContracts.ToDictionary(x => x.AssetPairId,
                    x => x.ScheduleSettings.Except(invalidSchedules.TryGetValue(x.AssetPairId, out var invalid)
                            ? invalid
                            : new List<CompiledScheduleSettingsContract>())
                        .Select(ScheduleSettings.Create).ToList());
                _compiledScheduleTimelineCache =
                    new Dictionary<string, List<CompiledScheduleTimeInterval>>();
                _lastCacheRecalculationTime = _dateService.Now();
            }
            catch (Exception exception)
            {
                await _log.WriteErrorAsync(nameof(ScheduleSettingsCacheService), nameof(UpdateSettingsAsync), exception);
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

        public Dictionary<string, List<CompiledScheduleTimeInterval>> GetCompiledScheduleSettings(
            DateTime currentDateTime, TimeSpan scheduleCutOff)
        {
            _readerWriterLockSlim.EnterReadLock();
            
            EnsureCacheValidUnsafe(currentDateTime);

            if (!_compiledScheduleTimelineCache.Any())
            {
                CacheWarmUp();
            }

            try
            {
                return _compiledScheduleTimelineCache;
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
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
            
            var scheduleSettingsByType = scheduleSettings
                .GroupBy(x => x.Start.GetConstraintType())
                .ToDictionary(x => x.Key, value => value);
            
            //handle weekly
            var weekly = scheduleSettingsByType.TryGetValue(ScheduleConstraintType.Weekly, out var weeklySchedule)
                ? weeklySchedule.SelectMany(sch =>
                {
                    var currentStart = GetCurrentWeekday(currentDateTime, sch.Start.DayOfWeek.Value)
                        .Add(sch.Start.Time.Subtract(scheduleCutOff));
                    var currentEnd = GetCurrentWeekday(currentDateTime, sch.End.DayOfWeek.Value)
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

            _readerWriterLockSlim.EnterWriteLock();
            try
            {
                var resultingTimeIntervals = weekly.Concat(single).Concat(daily).ToList();
                
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

        private static DateTime GetCurrentWeekday(DateTime start, DayOfWeek day)
        {
            return start.Date.AddDays((int) day - (int) start.DayOfWeek);
        }
        
        private static Dictionary<string, List<CompiledScheduleSettingsContract>> GetInvalidSchedules(
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
    }
}