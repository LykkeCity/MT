using System;
using MarginTrading.SettingsService.Contracts.Scheduling;

namespace MarginTrading.Backend.Core.DayOffSettings
{
    public class ScheduleSettings
    {
        public string Id { get; set; }
        public int Rank { get; set; }
        public bool? IsTradeEnabled { get; set; } = false;
        public TimeSpan? PendingOrdersCutOff { get; set; }

        public ScheduleConstraint Start { get; set; }
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
                    Date = DateTime.TryParse(scheduleSettingsContract.Start.Date, out var start) ? start : (DateTime?)null,
                    DayOfWeek = scheduleSettingsContract.Start.DayOfWeek,
                    Time = scheduleSettingsContract.Start.Time
                },
                End = new ScheduleConstraint
                {
                    Date = DateTime.TryParse(scheduleSettingsContract.End.Date, out var end) ? end : (DateTime?)null,
                    DayOfWeek = scheduleSettingsContract.End.DayOfWeek,
                    Time = scheduleSettingsContract.End.Time,
                }
            };
        }
    }
}