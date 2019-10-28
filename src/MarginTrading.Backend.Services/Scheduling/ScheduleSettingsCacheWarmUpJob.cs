// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using FluentScheduler;
using JetBrains.Annotations;
using MarginTrading.Backend.Services.AssetPairs;

namespace MarginTrading.Backend.Services.Scheduling
{
    [UsedImplicitly]
    public class ScheduleSettingsCacheWarmUpJob : IJob, IDisposable
    {
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;

        public ScheduleSettingsCacheWarmUpJob(
            IScheduleSettingsCacheService scheduleSettingsCacheService)
        {
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
        }
        
        public void Execute()
        {
            _scheduleSettingsCacheService.CacheWarmUpIncludingValidation();
            _scheduleSettingsCacheService.PlatformCacheWarmUp();
        }

        public void Dispose()
        {
        }
    }
}