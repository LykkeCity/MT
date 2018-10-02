using System;

namespace MarginTrading.Backend.Core.DayOffSettings
{
    public class CompiledScheduleTimeInterval
    {
        public ScheduleSettings Schedule { get; }
        public DateTime Start { get; }
        public DateTime End { get; }

        public CompiledScheduleTimeInterval(ScheduleSettings schedule, DateTime start, DateTime end)
        {
            Schedule = schedule;
            Start = start;
            End = end;
        }
    }
}