using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Extensions;
using MarginTrading.SettingsService.Contracts;
using MarginTrading.SettingsService.Contracts.Scheduling;

namespace MarginTrading.Backend.Services.AssetPairs
{
    internal class ScheduleSettingsCacheService : IScheduleSettingsCacheService
    {
        private readonly IScheduleSettingsApi _scheduleSettingsApi;
        private readonly IAssetPairsCache _assetPairsCache;
        private Dictionary<string, List<ScheduleSettings>> _cache;

        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();

        public ScheduleSettingsCacheService(
            IScheduleSettingsApi scheduleSettingsApi,
            IAssetPairsCache assetPairsCache)
        {
            _scheduleSettingsApi = scheduleSettingsApi;
            _assetPairsCache = assetPairsCache;

            UpdateSettingsAsync().GetAwaiter().GetResult();//called by IoC on init
        }

        public async Task UpdateSettingsAsync()
        {
            var newScheduleContract = await _scheduleSettingsApi.StateList(_assetPairsCache.GetAllIds().ToArray());

            _readerWriterLockSlim.EnterWriteLock();

            try
            {
                _cache = newScheduleContract.ToDictionary(x => x.AssetPairId,
                    x => x.ScheduleSettings.Select(ScheduleSettings.Create).ToList());
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        public List<ScheduleSettings> GetScheduleSettings(string assetPairId)
        {
            _readerWriterLockSlim.EnterReadLock();

            try
            {
                return !string.IsNullOrEmpty(assetPairId) && _cache.TryGetValue(assetPairId, out var settings) 
                    ? settings 
                    : new List<ScheduleSettings>();
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }
    }
}