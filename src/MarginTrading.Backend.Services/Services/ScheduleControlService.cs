// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using FluentScheduler;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Common.Services;
using MoreLinq;

namespace MarginTrading.Backend.Services.Services
{
    public class ScheduleControlService : IScheduleControlService
    {
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;
        private readonly ILog _log;
        private readonly IDateService _dateService;

        public ScheduleControlService(IScheduleSettingsCacheService scheduleSettingsCacheService, ILog log, IDateService dateService)
        {
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
            _log = log;
            _dateService = dateService;
        }
        
        /// <inheritdoc />
        public void ScheduleNext()
        {
            var currentDateTime = _dateService.Now();
            
            //need to know in which OvernightMarginJobType we are now and the moment of next change
            var marketsSchedule = _scheduleSettingsCacheService.GetMarketsTradingSchedule();

            var marketsToHandle = TryGetClosestPoint(marketsSchedule, currentDateTime, out var nextStart);

            if (nextStart == default)
            {
                _log.WriteFatalErrorAsync(nameof(ScheduleControlService), nameof(ScheduleNext),
                        new Exception(
                            $"Incorrect markets schedule! Need to fix it and restart the service. Check time: [{currentDateTime:s}], detected markets to handle: [{marketsToHandle.ToJson()}]"))
                    .Wait();
                return;
            }
            
            _log.WriteInfo(nameof(ScheduleControlService), nameof(ScheduleNext),
                $"Planning next check to [{nextStart:s}]."
                + $" Check time: [{currentDateTime:s}]." 
                + $" Markets to handle: [{marketsToHandle.ToJson()}]");
            
            _scheduleSettingsCacheService.HandleMarketStateChanges(currentDateTime, marketsToHandle);
            
            JobManager.AddJob(ScheduleNext, s => s
                .WithName(nameof(ScheduleControlService)).NonReentrant().ToRunOnceAt(nextStart));
        }

        private string[] TryGetClosestPoint(Dictionary<string, List<CompiledScheduleTimeInterval>> marketsSchedule, 
            DateTime currentDateTime, out DateTime nextStart)
        {
            var intervals = new Dictionary<string, (DateTime Start, DateTime End)>();

            foreach (var (marketId, compiledScheduleTimeIntervals) in marketsSchedule)
            {
                if (!compiledScheduleTimeIntervals.All(x => x.Enabled())
                    && TryGetOperatingInterval(compiledScheduleTimeIntervals, currentDateTime, out var interval))
                {
                    intervals.Add(marketId, interval);
                }
            }
            
            nextStart = intervals.Values.SelectMany(x => new [] {x.Start, x.End})
                .Where(x => x.Subtract(currentDateTime).TotalSeconds > 1)
                .Min();

            return intervals
                .ToDictionary(x => x.Key, x =>
                    Math.Min(Math.Abs(x.Value.Start.Subtract(currentDateTime).TotalSeconds),
                        Math.Abs(x.Value.End.Subtract(currentDateTime).TotalSeconds)))
                .Where(x => x.Value < 1)
                .Select(x => x.Key)
                .ToArray();
        }

        private bool TryGetOperatingInterval(List<CompiledScheduleTimeInterval> compiledScheduleTimeIntervals,
            DateTime currentDateTime, out (DateTime Start, DateTime End) resultingInterval)
        {
            //no disabled intervals ahead => false
            if (!compiledScheduleTimeIntervals.Any(x => x.End > currentDateTime && !x.Enabled()))
            {
                resultingInterval = default;
                return false;
            }

            //add "all-the-time-enabled" time interval with min rank
            compiledScheduleTimeIntervals.Add(new CompiledScheduleTimeInterval(
                new ScheduleSettings { Rank = int.MinValue, IsTradeEnabled = true, }, 
                DateTime.MinValue, DateTime.MaxValue));

            //find current active interval
            var currentActiveInterval = compiledScheduleTimeIntervals
                .Where(x => x.Start <= currentDateTime && x.End > currentDateTime)
                .MaxBy(x => x.Schedule.Rank);

            //find changing time: MIN(end of current, start of next with higher Rank)
            //if the same state => continue
            //      other state => ON to OFF => Warn, Start, End
            //                     OFF to ON => End & Warn, Start => DateTime.Min
            DateTime? endOfInterval = null;
            foreach (var nextWithHigherRank in compiledScheduleTimeIntervals.Where(x => x.Start > currentDateTime
                                                              && x.Start < currentActiveInterval.End
                                                              && x.Schedule.Rank > currentActiveInterval.Schedule.Rank)
                .OrderBy(x => x.Start)
                .ThenByDescending(x => x.Schedule.Rank))
            {   
                //if the state is the same - it changes nothing but shifts start border
                if (currentActiveInterval.Enabled() == nextWithHigherRank.Enabled())
                {
                    endOfInterval = nextWithHigherRank.End;
                    continue;
                }

                //if interval end before the shifted border we can skip it
                if (endOfInterval.HasValue && nextWithHigherRank.End < endOfInterval.Value)
                {
                    continue;
                }

                if (nextWithHigherRank.Enabled())
                {
                    resultingInterval = (DateTime.MinValue, nextWithHigherRank.Start);
                    return true;
                }

                var resultingStart = endOfInterval ?? nextWithHigherRank.Start;

                resultingInterval = (resultingStart, nextWithHigherRank.End);
                return true;
            }
            
            resultingInterval = (DateTime.MinValue, currentActiveInterval.End);
            return true;
        }
    }
}