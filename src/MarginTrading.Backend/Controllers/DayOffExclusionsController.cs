using System;
using System.Collections.Generic;
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

        [HttpGet]
        public IReadOnlyList<DayOffExclusionContract> List()
        {
            return _dayOffSettingsService.GetExclusions().Values.Select(Convert).ToList();
        }
        
        [HttpGet]
        [CanBeNull]
        [Route("{id}")]
        public DayOffExclusionContract Get(Guid id)
        {
            return Convert(_dayOffSettingsService.GetExclusion(id));
        }
        
        [HttpPost]
        public DayOffExclusionContract Create([FromBody] DayOffExclusionContract contract)
        {
            return Convert(_dayOffSettingsService.CreateExclusion(Convert(contract)));
        }
        
        [HttpPut]
        public DayOffExclusionContract Update([FromBody] DayOffExclusionContract contract)
        {
            return Convert(_dayOffSettingsService.UpdateExclusion(Convert(contract)));
        }

        [ContractAnnotation("dayOffExclusion:null => null")]
        private static DayOffExclusionContract Convert([CanBeNull] DayOffExclusion dayOffExclusion)
        {
            if (dayOffExclusion == null)
            {
                return null;
            }
            
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
            return new DayOffExclusion(dayOffExclusion.Id, dayOffExclusion.AssetPairRegex, dayOffExclusion.Start,
                dayOffExclusion.End, dayOffExclusion.IsTradeEnabled);
        }
    }
}