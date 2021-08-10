// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
    /// <summary>
    /// Api to retrieve compiled trading schedule - for cache initialization only.
    /// </summary>
    [Authorize]
    [Route("api/trading-schedule")]
    public class TradingScheduleController : Controller, ITradingScheduleApi
    {
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;
        private readonly IAssetPairDayOffService _assetPairDayOffService;
        private readonly IAssetPairsCache _assetPairsCache;
        
        public TradingScheduleController(
            IDateService dateService,
            IScheduleSettingsCacheService scheduleSettingsCacheService,
            IAssetPairDayOffService assetPairDayOffService,
            IAssetPairsCache assetPairsCache)
        {
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
            _assetPairDayOffService = assetPairDayOffService;
            _assetPairsCache = assetPairsCache;
        }

        /// <summary>
        /// Get current compiled trading schedule for each asset pair in a form of the list of time intervals.
        /// Cache is invalidated and recalculated after 00:00:00.000 each day on request. 
        /// </summary>
        [HttpGet("compiled")]
        [HttpGet("asset-pairs-compiled")]
        [Obsolete("Markets schedule should be used")]
        public Task<Dictionary<string, List<CompiledScheduleTimeIntervalContract>>> CompiledTradingSchedule()
        {
            return Task.FromResult(
                _scheduleSettingsCacheService.GetCompiledAssetPairScheduleSettings()
                    .ToDictionary(x => x.Key, x => x.Value.Select(ti => ti.ToRabbitMqContract()).ToList()));
        }

        /// <summary>
        /// Get current compiled trading schedule for each market.
        /// </summary>
        [HttpGet("markets-compiled")]
        public Task<Dictionary<string, List<CompiledScheduleTimeIntervalContract>>> CompiledMarketTradingSchedule()
        {
            return Task.FromResult(_scheduleSettingsCacheService.GetMarketsTradingSchedule()
                .ToDictionary(x => x.Key, x => x.Value.Select(m => m.ToRabbitMqContract()).ToList()));
        }

        /// <summary>
        /// Get current instrument's trading status.
        /// Do not use this endpoint from FrontEnd!
        /// </summary>
        [HttpGet("is-enabled/{assetPairId}")]
        [Obsolete("Markets schedule should be used")]
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
        [Obsolete("Markets schedule should be used")]
        public Task<Dictionary<string, bool>> AreInstrumentsEnabled()
        {
            var assetPairIds = _assetPairsCache.GetAllIds();

            var areEnabled = assetPairIds.ToDictionary(x => x, x => !_assetPairDayOffService.IsDayOff(x));
            
            return Task.FromResult(areEnabled);
        }

        /// <summary>
        /// Get current markets state: trading enabled or disabled.
        /// </summary>
        [HttpGet("markets-state")]
        public Task<Dictionary<string, bool>> MarketsState()
        {
            return Task.FromResult(_scheduleSettingsCacheService.GetMarketState()
                .ToDictionary(x => x.Key, x => x.Value.IsEnabled));
        }
    }
}