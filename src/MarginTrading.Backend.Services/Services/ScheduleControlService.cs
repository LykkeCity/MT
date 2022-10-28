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
        private readonly IScheduleSettingsCacheService _scheduleSettingsCache;
        private readonly ILog _log;
        private readonly IDateService _dateService;
        
        private static readonly object LockObj = new object();

        public ScheduleControlService(IScheduleSettingsCacheService scheduleSettingsCache, ILog log, IDateService dateService)
        {
            _scheduleSettingsCache = scheduleSettingsCache;
            _log = log;
            _dateService = dateService;
        }
        
        /// <inheritdoc />
        public void ScheduleNext()
        {
            lock (LockObj)
            {
                var currentDateTime = _dateService.Now();

                //need to know in which OvernightMarginJobType we are now and the moment of next change
                var marketsSchedule = _scheduleSettingsCache.GetMarketsTradingSchedule();

                var nextStart = TryGetClosestPoint(marketsSchedule, currentDateTime);

                _log.WriteInfo(nameof(ScheduleControlService), nameof(ScheduleNext),
                    $"Planning next check to [{nextStart:s}]."
                    + $" Check time: [{currentDateTime:s}].");

                _scheduleSettingsCache.HandleMarketStateChanges(currentDateTime);

                JobManager.RemoveJob(nameof(ScheduleControlService));
                JobManager.AddJob(ScheduleNext, s => s
                    .WithName(nameof(ScheduleControlService)).NonReentrant().ToRunOnceAt(nextStart));
            }
        }

        public DateTime TryGetClosestPoint(Dictionary<string, List<CompiledScheduleTimeInterval>> marketsSchedule, 
            DateTime currentDateTime)
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

            var followingPoints = intervals.Values.SelectMany(x => new[] {x.Start, x.End})
                .Where(x => x > currentDateTime)
                .ToList();
            return followingPoints.Any() ? followingPoints.Min() : currentDateTime.AddDays(1);
        }

        public bool TryGetOperatingInterval(List<CompiledScheduleTimeInterval> compiledScheduleTimeIntervals,
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
            var currentActiveInterval = MoreEnumerable.MaxBy(compiledScheduleTimeIntervals
                .Where(x => x.Start <= currentDateTime && x.End > currentDateTime),
                x => x.Schedule.Rank).First();

            //find changing time: MIN(end of current, start of next with higher Rank)
            //if the same state => continue
            //      other state => ON to OFF => Start, End
            //                     OFF to ON => End, Start => DateTime.Min
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