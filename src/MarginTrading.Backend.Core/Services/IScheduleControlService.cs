// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.Services
{
    /// <summary>
    /// Service to control trading schedule
    /// </summary>
    public interface IScheduleControlService
    {
        /// <summary>
        /// Act and schedule the next job: warning, start or end. Job type depends on the next state.
        /// </summary>
        void ScheduleNext();
    }
}