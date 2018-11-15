using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.TradingSchedule;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    /// <inheritdoc cref="ITradingScheduleApi"/>
    [Authorize]
    [Route("api/trading-schedule")]
    public class TradingScheduleController : Controller, ITradingScheduleApi
    {
        private readonly IDateService _dateService;
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;
        
        public TradingScheduleController(
            IDateService dateService,
            IScheduleSettingsCacheService scheduleSettingsCacheService)
        {
            _dateService = dateService;
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
        }

        /// <inheritdoc />
        [HttpGet("compiled")]
        public Dictionary<string, List<CompiledScheduleTimeIntervalContract>> GetCompiledTradingSchedule()
        {
            return _scheduleSettingsCacheService.GetCompiledScheduleSettings(_dateService.Now(), TimeSpan.Zero)
                .ToDictionary(x => x.Key, x => x.Value.Select(ti => ti.ToRabbitMqContract()).ToList());
        }
    }
}