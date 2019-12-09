// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.SettingsService.Contracts.Scheduling;

namespace MarginTrading.Backend.Core.Extensions
{
    public static class ScheduleSettingsExtensions
    {
        public static bool Enabled(this CompiledScheduleTimeInterval compiledScheduleTimeInterval)
        {
            return compiledScheduleTimeInterval?.Schedule.IsTradeEnabled ?? true;
        }

        public static Dictionary<string, List<CompiledScheduleSettingsContract>> InvalidSchedules(
            this IEnumerable<CompiledScheduleContract> scheduleContracts)
        {
            var invalidSchedules = new Dictionary<string, List<CompiledScheduleSettingsContract>>();
            foreach (var scheduleContract in scheduleContracts)
            {
                var scheduleSettings = new List<CompiledScheduleSettingsContract>();
                foreach (var scheduleSetting in scheduleContract.ScheduleSettings)
                {
                    try
                    {
                        ScheduleConstraintContract.Validate(scheduleSetting);
                    }
                    catch
                    {
                        scheduleSettings.Add(scheduleSetting);
                    }
                }

                if (scheduleSettings.Any())
                {
                    invalidSchedules.Add(scheduleContract.AssetPairId, scheduleSettings);
                }
            }

            return invalidSchedules;
        }

        public static List<ScheduleSettingsContract> InvalidSchedules(
            this IEnumerable<ScheduleSettingsContract> scheduleContracts)
        {
            var scheduleSettings = new List<ScheduleSettingsContract>();
            foreach (var scheduleSetting in scheduleContracts)
            {
                try
                {
                    ScheduleConstraintContract.Validate(scheduleSetting);
                }
                catch
                {
                    scheduleSettings.Add(scheduleSetting);
                }
            }

            return scheduleSettings;
        }

        public static IEnumerable<ScheduleSettingsContract> ConcatWithPlatform(
            this IEnumerable<ScheduleSettingsContract> targetSchedule, IEnumerable<ScheduleSettingsContract> platformSchedule)
        {
            var rank = int.MaxValue;
            var prev = 0;
            var resultingPlatformSchedule = platformSchedule
                .OrderByDescending(x => x.Rank)
                .Select(x =>
                {
                    var result = x.CloneWithRank(x.Rank == prev ? rank : --rank);//todo add some protection to settings service

                    prev = x.Rank;

                    return result;
                });

            return targetSchedule.Concat(resultingPlatformSchedule);
        }

        public static ScheduleSettingsContract CloneWithRank(this ScheduleSettingsContract schedule, int rank)
            => new ScheduleSettingsContract
            {
                Id = schedule.Id,
                Rank = rank,
                AssetPairRegex = schedule.AssetPairRegex,
                AssetPairs = schedule.AssetPairs,
                MarketId = schedule.MarketId,
                IsTradeEnabled = schedule.IsTradeEnabled,
                PendingOrdersCutOff = schedule.PendingOrdersCutOff,
                Start = schedule.Start.Clone(),
                End = schedule.End.Clone(),
            };

        private static ScheduleConstraintContract Clone(this ScheduleConstraintContract constraint)
            => new ScheduleConstraintContract
            {
                Date = constraint.Date,
                DayOfWeek = constraint.DayOfWeek,
                Time = constraint.Time,
            };

        public static MarketState GetMarketState(
            this List<CompiledScheduleTimeInterval> compiledSchedule, string marketId, DateTime currentDateTime)
        {
            var currentInterval = compiledSchedule
                .Where(x => IsBetween(currentDateTime, x.Start, x.End))
                .OrderByDescending(x => x.Schedule.Rank)
                .FirstOrDefault();
            
            var isEnabled = currentInterval.Enabled();
            var lastTradingDay = GetPreviousTradingDay(compiledSchedule, currentInterval, currentDateTime);
            var nextTradingDay = GetNextTradingDay(compiledSchedule, currentInterval, currentDateTime, lastTradingDay);  

            var result = new MarketState
            {
                Id = marketId,
                IsEnabled = isEnabled,
                NextChange = nextTradingDay,
            };

            return result;
        }

        private static bool IsBetween(DateTime currentDateTime, DateTime start, DateTime end)
        {
            return start <= currentDateTime && currentDateTime < end;
        }
        
        private static DateTime GetPreviousTradingDay(List<CompiledScheduleTimeInterval>
            compiledSchedule, CompiledScheduleTimeInterval currentInterval, DateTime currentDateTime)
        {
            if (currentInterval.Enabled())
                return currentDateTime.Date;
            
            var timestampBeforeCurrentIntervalStart = currentInterval.Start.AddTicks(-1);

            // search for the interval just before the current interval started
            var previousInterval = compiledSchedule
                .Where(x => IsBetween(timestampBeforeCurrentIntervalStart, x.Start, x.End))
                .OrderByDescending(x => x.Schedule.Rank)
                .FirstOrDefault();

            // if trading was enabled, then at that moment was the last trading day
            if (previousInterval.Enabled())
                return timestampBeforeCurrentIntervalStart.Date;

            // if no, there was one more disabled interval and we should go next
            return GetPreviousTradingDay(compiledSchedule, previousInterval, previousInterval.Start);
        }

        private static DateTime GetNextTradingDay(List<CompiledScheduleTimeInterval>
            compiledSchedule, CompiledScheduleTimeInterval currentInterval, DateTime currentDateTime, DateTime lastTradingDay)
        {
            // search for the interval right after the current interval finished
            var ordered = compiledSchedule
                .Where(x => x.End > (currentInterval?.End ?? currentDateTime)
                            || currentInterval != null && x.Schedule.Rank > currentInterval.Schedule.Rank &&
                            x.End > currentInterval.End)
                .OrderBy(x => x.Start)
                .ThenByDescending(x => x.Schedule.Rank)
                .ToList();
            
            var nextInterval = ordered.FirstOrDefault();
            
            if (nextInterval == null)
            {
                if (!currentInterval.Enabled() && currentInterval.End.Date > lastTradingDay.Date)
                {
                    return currentInterval.End;
                }
                else // means no any intervals (current or any in the future)
                {
                    return currentDateTime.Date.AddDays(1); 
                }
            }

            var stateIsChangedToEnabled = nextInterval.Schedule.IsTradeEnabled != currentInterval.Enabled() && nextInterval.Enabled();
            var intervalIsMissing = currentInterval != null && nextInterval.Start > currentInterval.End;

            if (stateIsChangedToEnabled || intervalIsMissing && currentInterval.End.Date > lastTradingDay.Date)
            {
                // ReSharper disable once PossibleNullReferenceException
                // if status was changed and next is enabled, that means current interval is disable == it not null
                return currentInterval.End;
            }

            // if we have long enabled interval with overnight, next day will start at 00:00:00
            if (currentInterval.Enabled() && currentDateTime.Date.AddDays(1) < nextInterval.Start)
            {
                return currentDateTime.Date.AddDays(1);
            }

            return GetNextTradingDay(compiledSchedule, nextInterval, nextInterval.End.AddTicks(1), lastTradingDay);
        }
    }
}