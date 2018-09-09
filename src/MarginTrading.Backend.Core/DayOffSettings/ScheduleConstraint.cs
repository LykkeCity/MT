using System;

namespace MarginTrading.Backend.Core.DayOffSettings
{
    public class ScheduleConstraint
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
    }
}