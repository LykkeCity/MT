using System;

namespace MarginTrading.Backend.Core.DayOffSettings
{
    public class ScheduleConstraint
    {
        public DateTime? Date { get; set; } //if not set => recurring schedule
        public DayOfWeek? DayOfWeek { get; set; } 
        public TimeSpan Time { get; set; }
    }
}