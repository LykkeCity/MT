using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.DayOffSettings;

namespace MarginTrading.Backend.Core.Services
{
    /// <summary>
    /// Service to manage overnight margin parameter
    /// </summary>
    public interface IOvernightMarginService
    {
        /// <summary>
        /// Act and schedule the next job: warning, start or end. Job type depends on the next state.
        /// </summary>
        void ScheduleNext();
    }
}