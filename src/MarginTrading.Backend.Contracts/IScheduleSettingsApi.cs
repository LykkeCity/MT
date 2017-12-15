using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.DayOffSettings;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IScheduleSettingsApi
    {
        [Get("/api/ScheduleSettings")]
        Task<ScheduleSettingsContract> Get();
        
        [Put("/api/ScheduleSettings")]
        Task<ScheduleSettingsContract> Set([Body] ScheduleSettingsContract scheduleSettingsContract);
    }
}