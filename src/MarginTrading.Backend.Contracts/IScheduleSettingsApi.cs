using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.DayOffSettings;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// Manages day offs schedule and exclusions
    /// </summary>
    [PublicAPI]
    public interface IScheduleSettingsApi
    {
        /// <summary>
        /// Gets schedule settings
        /// </summary>
        [Get("/api/ScheduleSettings")]
        Task<ScheduleSettingsContract> GetSchedule();
        
        /// <summary>
        /// Sets schedule settings
        /// </summary>
        [Put("/api/ScheduleSettings")]
        Task<ScheduleSettingsContract> SetSchedule([Body] ScheduleSettingsContract scheduleSettingsContract);
        
        /// <summary>
        /// Get all exclusions
        /// </summary>
        [Get("/api/ScheduleSettings/Exclusions")]
        Task<IReadOnlyList<DayOffExclusionContract>> ListExclusions();

        /// <summary>
        /// Get exclusion by id
        /// </summary>        
        [Get("/api/ScheduleSettings/Exclusions/{id}")]
        Task<DayOffExclusionContract> GetExclusion(Guid id);

        /// <summary>
        /// Get all compiled exclusions
        /// </summary>
        [Get("/api/ScheduleSettings/Exclusions/Compiled")]
        Task<CompiledExclusionsListContract> ListCompiledExclusions();

        /// <summary>
        /// Create exclusion
        /// </summary> 
        [Post("/api/ScheduleSettings/Exclusions")]
        Task<DayOffExclusionContract> CreateExclusion([Body] DayOffExclusionInputContract contract);

        /// <summary>
        /// Update exclusion
        /// </summary>
        [Put("/api/ScheduleSettings/Exclusions/{id}")]
        Task<DayOffExclusionContract> UpdateExclusion(Guid id, [Body] DayOffExclusionInputContract contract);

        /// <summary>
        /// Delete exclusion
        /// </summary>
        [Delete("/api/ScheduleSettings/Exclusions/{id}")]
        Task DeleteExclusion(Guid id);
    }
}