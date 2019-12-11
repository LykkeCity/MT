// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.Services
{
    /// <summary>
    /// Service to control trading schedule
    /// </summary>
    public interface IScheduleControlService
    {
        void ScheduleNext();
    }
}