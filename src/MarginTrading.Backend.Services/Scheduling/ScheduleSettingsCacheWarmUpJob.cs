using System;
using FluentScheduler;
using MarginTrading.Backend.Services.AssetPairs;

namespace MarginTrading.Backend.Services.Scheduling
{
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
            _scheduleSettingsCacheService.CacheWarmUp();
        }

        public void Dispose()
        {
        }
    }
}