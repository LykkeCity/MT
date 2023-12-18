// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Common.Services;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly MarginTradingSettings _settings;
        private readonly IDateService _dateService;
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;

        public IsAliveController(
            MarginTradingSettings settings,
            IDateService dateService,
            IScheduleSettingsCacheService scheduleSettingsCacheService)
        {
            _settings = settings;
            _dateService = dateService;
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
        }
        
        [HttpGet]
        public IsAliveResponse Get()
        {
            return new IsAliveResponse
            {
                Version =
                    Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion,
                Env = _settings.Env,
                ServerTime = _dateService.Now()
            };
        }

        [HttpGet("temp")]
        public List<CompiledScheduleTimeInterval> GetTemp()
        {
            return _scheduleSettingsCacheService.GetPlatformTradingSchedule();
        }
    }
}
