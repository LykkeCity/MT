using System;

namespace MarginTrading.Backend.Core.DayOffSettings
{
    public class ScheduleConstraint : IComparable
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
            //todo what about yearly?

            return ScheduleConstraintType.Invalid;
        }

        public int CompareTo(object obj)
        {
            return obj is ScheduleConstraint second
                ? (Date == second.Date
                   && DayOfWeek == second.DayOfWeek
                   && Time == second.Time
                    ? 0
                    : 1)
                : -1;
        }
    }
}