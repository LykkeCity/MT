using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Contract.BackendContracts.DayOffSettings;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Route("api/[controller]")]
    public class DayOffExclusionsController : Controller
    {
        private readonly IDayOffSettingsService _dayOffSettingsService;

        public DayOffExclusionsController(IDayOffSettingsService dayOffSettingsService)
        {
            _dayOffSettingsService = dayOffSettingsService;
        }

        /// <summary>
        /// Get all exclusions
        /// </summary>
        [HttpGet]
        public IReadOnlyList<DayOffExclusionContract> List()
        {
            return _dayOffSettingsService.GetExclusions().Values.Select(Convert).ToList();
        }

        /// <summary>
        /// Get exclusion by id
        /// </summary>        
        [HttpGet]
        [CanBeNull]
        [Route("{id}")]
        public DayOffExclusionContract Get(Guid id)
        {
            return Convert(_dayOffSettingsService.GetExclusion(id));
        }

        /// <summary>
        /// Get all compiled exclusions
        /// </summary>
        [HttpGet]
        [Route("compiled")]
        public CompiledExclusionsListContract ListCompiled()
        {
            return Convert(_dayOffSettingsService.GetCompiledExclusions());
        }

        /// <summary>
        /// Create exclusion
        /// </summary> 
        [HttpPost]
        public DayOffExclusionContract Create([FromBody] DayOffExclusionContract contract)
        {
            return Convert(_dayOffSettingsService.CreateExclusion(Convert(contract)));
        }

        /// <summary>
        /// Update exclusion
        /// </summary>
        [HttpPut]
        public DayOffExclusionContract Update([FromBody] DayOffExclusionContract contract)
        {
            return Convert(_dayOffSettingsService.UpdateExclusion(Convert(contract)));
        }

        /// <summary>
        /// Delete exclusions
        /// </summary>
        [HttpDelete]
        [Route("{id}")]
        public OkResult Delete(Guid id)
        {
            _dayOffSettingsService.DeleteExclusion(id);
            return Ok();
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

        private static DayOffExclusion Convert(DayOffExclusionContract dayOffExclusion)
        {
            if (dayOffExclusion == null) throw new ArgumentNullException(nameof(dayOffExclusion));
            return new DayOffExclusion(dayOffExclusion.Id, dayOffExclusion.AssetPairRegex, dayOffExclusion.Start,
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