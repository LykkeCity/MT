using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.DayOffSettings;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Services.AssetPairs;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Route("api/[controller]")]
    public class ScheduleSettingsController : Controller, IScheduleSettingsApi
    {
        private readonly IDayOffSettingsService _dayOffSettingsService;

        public ScheduleSettingsController(IDayOffSettingsService dayOffSettingsService)
        {
            _dayOffSettingsService = dayOffSettingsService;
        }

        /// <summary>
        /// Get all exclusions
        /// </summary>
        [HttpGet]
        [Route("Exclusions")]
        public Task<IReadOnlyList<DayOffExclusionContract>> ListExclusions()
        {
            return Task.FromResult<IReadOnlyList<DayOffExclusionContract>>(_dayOffSettingsService.GetExclusions().Values.Select(Convert).ToList());
        }

        /// <summary>
        /// Get exclusion by id
        /// </summary>        
        [HttpGet]
        [CanBeNull]
        [Route("Exclusions/{id}")]
        public Task<DayOffExclusionContract> GetExclusion(Guid id)
        {
            return Task.FromResult(Convert(_dayOffSettingsService.GetExclusion(id)));
        }

        /// <summary>
        /// Get all compiled exclusions
        /// </summary>
        [HttpGet]
        [Route("Exclusions/Compiled")]
        public Task<CompiledExclusionsListContract> ListCompiledExclusions()
        {
            return Task.FromResult(Convert(_dayOffSettingsService.GetCompiledExclusions()));
        }

        /// <summary>
        /// Create exclusion
        /// </summary> 
        [HttpPost]
        [Route("Exclusions")]
        public Task<DayOffExclusionContract> CreateExclusion([FromBody] DayOffExclusionInputContract contract)
        {
            return Task.FromResult(Convert(_dayOffSettingsService.CreateExclusion(Convert(Guid.NewGuid(), contract))));
        }

        /// <summary>
        /// Update exclusion
        /// </summary>
        [HttpPut]
        [Route("Exclusions/{id}")]
        public Task<DayOffExclusionContract> UpdateExclusion(Guid id, [FromBody] DayOffExclusionInputContract contract)
        {
            return Task.FromResult(Convert(_dayOffSettingsService.UpdateExclusion(Convert(id, contract))));
        }

        /// <summary>
        /// Delete exclusion
        /// </summary>
        [HttpDelete]
        [Route("Exclusions/{id}")]
        public Task DeleteExclusion(Guid id)
        {
            _dayOffSettingsService.DeleteExclusion(id);
            return Task.FromResult(Ok());
        }

        [HttpGet]
        public Task<ScheduleSettingsContract> GetSchedule()
        {
            return Task.FromResult(Convert(_dayOffSettingsService.GetScheduleSettings()));
        }

        [HttpPut]
        public Task<ScheduleSettingsContract> SetSchedule([FromBody] ScheduleSettingsContract scheduleSettingsContract)
        {
            return Task.FromResult(Convert(_dayOffSettingsService.SetScheduleSettings(Convert(scheduleSettingsContract))));
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

        [ContractAnnotation("dayOffExclusion:null => null")]
        private static DayOffExclusionContract Convert([CanBeNull] DayOffExclusion dayOffExclusion)
        {
            if (dayOffExclusion == null) return null;

            return new DayOffExclusionContract
            {
                Id = dayOffExclusion.Id,
                AssetPairRegex = dayOffExclusion.AssetPairRegex,
                Start = dayOffExclusion.Start,
                End = dayOffExclusion.End,
                IsTradeEnabled = dayOffExclusion.IsTradeEnabled,
            };
        }

        private static DayOffExclusion Convert(Guid id, DayOffExclusionInputContract dayOffExclusion)
        {
            if (dayOffExclusion == null) throw new ArgumentNullException(nameof(dayOffExclusion));
            return new DayOffExclusion(id, dayOffExclusion.AssetPairRegex, dayOffExclusion.Start,
                dayOffExclusion.End, dayOffExclusion.IsTradeEnabled);
        }

        private static CompiledExclusionsListContract Convert(ImmutableDictionary<string, ImmutableArray<DayOffExclusion>> compiledExclusions)
        {
            var converted = compiledExclusions.SelectMany(p => p.Value.Select(e =>
                    (e.IsTradeEnabled, Exclusion: new CompiledExclusionContract
                    {
                        Id = e.Id,
                        AssetPairId = p.Key,
                        AssetPairRegex = e.AssetPairRegex,
                        Start = e.Start,
                        End = e.End,
                    })))
                .ToLookup(t => t.IsTradeEnabled, t => t.Exclusion);
            
            return new CompiledExclusionsListContract
            {
                TradesDisabled = converted[false].OrderBy(e => e.AssetPairId).ToList(),
                TradesEnabled = converted[true].OrderBy(e => e.AssetPairId).ToList(),
            }; 
        }
    }
}