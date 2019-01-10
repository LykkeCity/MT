using System;
using FluentScheduler;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Services;

namespace MarginTrading.Backend.Services.Scheduling
{
    [UsedImplicitly]
    public class OvernightMarginJob : IJob, IDisposable
    {
        private readonly IOvernightMarginService _overnightMarginService;
        
        public OvernightMarginJob(
            IOvernightMarginService overnightMarginService)
        {
            _overnightMarginService = overnightMarginService;
        }
        
        public void Execute()
        {
            _overnightMarginService.ScheduleNext();
        }

        public void Dispose()
        {
        }
    }
}