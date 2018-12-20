using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.TradingSchedule;
using MarginTrading.Backend.Core;
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
            _assetPairsCache = assetPairsCache;
        }

        /// <inheritdoc cref="ITradingScheduleApi" />
        [HttpGet("compiled")]
        public Task<Dictionary<string, List<CompiledScheduleTimeIntervalContract>>> CompiledTradingSchedule()
        {
            return Task.FromResult(
                _scheduleSettingsCacheService.GetCompiledScheduleSettings(_dateService.Now(), TimeSpan.Zero)
                    .ToDictionary(x => x.Key, x => x.Value.Select(ti => ti.ToRabbitMqContract()).ToList()));
        }

        /// <inheritdoc cref="ITradingScheduleApi"/>
        [HttpGet("is-enabled/{assetPairId}")]
        public Task<bool> IsInstrumentEnabled(string assetPairId)
        {
            _assetPairsCache.GetAssetPairById(assetPairId);

            var isEnabled = !_assetPairDayOffService.IsDayOff(assetPairId);
            
            return Task.FromResult(isEnabled);
        }
        
        /// <inheritdoc cref="ITradingScheduleApi"/>
        [HttpGet("is-enabled")]
        public Task<Dictionary<string, bool>> AreInstrumentsEnabled()
        {
            var assetPairIds = _assetPairsCache.GetAllIds();

            var areEnabled = assetPairIds.ToDictionary(x => x, x => !_assetPairDayOffService.IsDayOff(x));
            
            return Task.FromResult(areEnabled);
        }
    }
}