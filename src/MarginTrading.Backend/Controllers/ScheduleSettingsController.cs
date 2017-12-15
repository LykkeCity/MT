using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.DayOffSettings;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Services.AssetPairs;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Route("api/[controller]")]
    public class ScheduleSettingsController : Controller
    {
        private readonly IDayOffSettingsService _dayOffSettingsService;

        public ScheduleSettingsController(IDayOffSettingsService dayOffSettingsService)
        {
            _dayOffSettingsService = dayOffSettingsService;
        }

        [HttpGet]
        public ScheduleSettingsContract Get()
        {
            return Convert(_dayOffSettingsService.GetScheduleSettings());
        }
        
        [HttpPut]
        public ScheduleSettingsContract Set([FromBody] ScheduleSettingsContract scheduleSettingsContract)
        {
            return Convert(_dayOffSettingsService.SetScheduleSettings(Convert(scheduleSettingsContract)));
        }

        [ContractAnnotation("shedule:null => null")]
        private static ScheduleSettingsContract Convert([CanBeNull] ScheduleSettings shedule)
        {
            if (shedule == null)
            {
                return null;
            }
            
            return new ScheduleSettingsContract
            {
                AssetPairsWithoutDayOff = shedule.AssetPairsWithoutDayOff,
                DayOffEndDay = shedule.DayOffEndDay,
                DayOffEndTime = shedule.DayOffEndTime,
                DayOffStartDay = shedule.DayOffStartDay,
                DayOffStartTime = shedule.DayOffStartTime,
                PendingOrdersCutOff = shedule.PendingOrdersCutOff,
            };
        }
        
        private static ScheduleSettings Convert(ScheduleSettingsContract shedule)
        {
            return new ScheduleSettings(
                dayOffStartDay: shedule.DayOffStartDay,
                dayOffStartTime: shedule.DayOffStartTime,
                dayOffEndDay: shedule.DayOffEndDay,
                dayOffEndTime: shedule.DayOffEndTime,
                assetPairsWithoutDayOff: shedule.AssetPairsWithoutDayOff,
                pendingOrdersCutOff: shedule.PendingOrdersCutOff);
        }
    }
}