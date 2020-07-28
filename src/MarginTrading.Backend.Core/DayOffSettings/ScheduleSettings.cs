// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MarginTrading.AssetService.Contracts.Scheduling;
// ReSharper disable NotNullMemberIsNotInitialized

namespace MarginTrading.Backend.Core.DayOffSettings
{
    public class ScheduleSettings
    {
        public string Id { get; set; }
        public int Rank { get; set; }
        public bool? IsTradeEnabled { get; set; } = false;
        public TimeSpan? PendingOrdersCutOff { get; set; }

        /// <summary>
        /// Can't be null. Must be validated before conversion from contracts.
        /// </summary>
        [NotNull]
        public ScheduleConstraint Start { get; set; }
        /// <summary>
        /// Can't be null. Must be validated before conversion from contracts.
        /// </summary>
        [NotNull]
        public ScheduleConstraint End { get; set; }

        public static ScheduleSettings Create(CompiledScheduleSettingsContract scheduleSettingsContract)
        {
            return new ScheduleSettings
            {
                Id = scheduleSettingsContract.Id,
                Rank = scheduleSettingsContract.Rank,
                IsTradeEnabled = scheduleSettingsContract.IsTradeEnabled,
                PendingOrdersCutOff = scheduleSettingsContract.PendingOrdersCutOff,
                Start = new ScheduleConstraint
                {
                    Date = scheduleSettingsContract.Start.Date,
                    DayOfWeek = scheduleSettingsContract.Start.DayOfWeek,
                    Time = scheduleSettingsContract.Start.Time
                },
                End = new ScheduleConstraint
                {
                    Date = scheduleSettingsContract.End.Date,
                    DayOfWeek = scheduleSettingsContract.End.DayOfWeek,
                    Time = scheduleSettingsContract.End.Time,
                }
            };
        }

        public static ScheduleSettings Create(ScheduleSettingsContract scheduleSettingsContract)
        {
            return new ScheduleSettings
            {
                Id = scheduleSettingsContract.Id,
                Rank = scheduleSettingsContract.Rank,
                IsTradeEnabled = scheduleSettingsContract.IsTradeEnabled,
                PendingOrdersCutOff = scheduleSettingsContract.PendingOrdersCutOff,
                Start = new ScheduleConstraint
                {
                    Date = scheduleSettingsContract.Start.Date,
                    DayOfWeek = scheduleSettingsContract.Start.DayOfWeek,
                    Time = scheduleSettingsContract.Start.Time
                },
                End = new ScheduleConstraint
                {
                    Date = scheduleSettingsContract.End.Date,
                    DayOfWeek = scheduleSettingsContract.End.DayOfWeek,
                    Time = scheduleSettingsContract.End.Time,
                }
            };
        }
    }
}