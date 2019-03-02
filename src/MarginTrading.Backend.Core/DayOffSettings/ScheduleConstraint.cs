using System;

namespace MarginTrading.Backend.Core.DayOffSettings
{
    public class ScheduleConstraint : IEquatable<ScheduleConstraint>
    {
        public DateTime? Date { get; set; }
        public DayOfWeek? DayOfWeek { get; set; } 
        public TimeSpan Time { get; set; }
        
        public ScheduleConstraintType GetConstraintType()
        {
            if (Date == null && DayOfWeek == default)
            {
                return ScheduleConstraintType.Daily;
            }
            if (Date != null && DayOfWeek == default)
            {
                return ScheduleConstraintType.Single;
            }
            if (Date == null && DayOfWeek != default)
            {
                return ScheduleConstraintType.Weekly;
            }

            return ScheduleConstraintType.Invalid;
        }

        public bool Equals(ScheduleConstraint other)
        {
            return other != null
                    && Date == other.Date
                    && DayOfWeek == other.DayOfWeek
                    && Time == other.Time;
        }
    }
}