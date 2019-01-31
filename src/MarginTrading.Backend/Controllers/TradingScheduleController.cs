using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.TradingSchedule;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Filters;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    /// <summary>
    /// Api to retrieve compiled trading schedule - for cache initialization only.
    /// </summary>
    [Authorize]
    [Route("api/trading-schedule")]
    public class TradingScheduleController : Controller, ITradingScheduleApi
    {
        private readonly IDateService _dateService;
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;
        private readonly IAssetPairDayOffService _assetPairDayOffService;
        private readonly IAssetPairsCache _assetPairsCache;
        
        public TradingScheduleController(
            IDateService dateService,
            IScheduleSettingsCacheService scheduleSettingsCacheService,
            IAssetPairDayOffService assetPairDayOffService,
            IAssetPairsCache assetPairsCache)
        {
            _dateService = dateService;
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
            _assetPairDayOffService = assetPairDayOffService;
            _assetPairsCache = assetPairsCache;
        }

        /// <summary>
        /// Get current compiled trading schedule for each asset pair in a form of the list of time intervals.
        /// Cache is invalidated and recalculated after 00:00:00.000 each day on request. 
        /// </summary>
        [HttpGet("compiled")]
        public Task<Dictionary<string, List<CompiledScheduleTimeIntervalContract>>> CompiledTradingSchedule()
        {
            return Task.FromResult(
                _scheduleSettingsCacheService.GetCompiledScheduleSettings(_dateService.Now(), TimeSpan.Zero)
                    .ToDictionary(x => x.Key, x => x.Value.Select(ti => ti.ToRabbitMqContract()).ToList()));
        }

        /// <summary>
        /// Get current instrument's trading status.
        /// Do not use this endpoint from FrontEnd!
        /// </summary>
        [HttpGet("is-enabled/{assetPairId}")]
        public Task<bool> IsInstrumentEnabled(string assetPairId)
        {
            _assetPairsCache.GetAssetPairById(assetPairId);

            var isEnabled = !_assetPairDayOffService.IsDayOff(assetPairId);
            
            return Task.FromResult(isEnabled);
        }
        
        /// <summary>
        /// Get current trading status of all instruments.
        /// Do not use this endpoint from FrontEnd!
        /// </summary>
        [HttpGet("are-enabled")]
        public Task<Dictionary<string, bool>> AreInstrumentsEnabled()
        {
            var assetPairIds = _assetPairsCache.GetAllIds();

            var areEnabled = assetPairIds.ToDictionary(x => x, x => !_assetPairDayOffService.IsDayOff(x));
            
            return Task.FromResult(areEnabled);
        }
    }
}