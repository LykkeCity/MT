// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.AssetService.Contracts.Scheduling;

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
            this IEnumerable<ScheduleSettingsContract> targetSchedule, 
            IEnumerable<ScheduleSettingsContract> platformSchedule,
            string platformKey)
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

            return targetSchedule
                .Where(x => x.Id != platformKey)
                .Concat(resultingPlatformSchedule);
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
            
            var isEnabled = currentInterval?.Enabled() ?? true;

            var result = new MarketState
            {
                Id = marketId,
                IsEnabled = isEnabled,
            };

            return result;
        }

        private static bool IsBetween(DateTime currentDateTime, DateTime start, DateTime end)
        {
            return start <= currentDateTime && currentDateTime < end;
        }
    }
}