using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.DayOffSettings;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IDayOffExclusionsApi
    {
        /// <summary>
        /// Get all exclusions
        /// </summary>
        [Get("/api/DayOffExclusions")]
        Task<IReadOnlyList<DayOffExclusionContract>> List();

        /// <summary>
        /// Get exclusion by id
        /// </summary>        
        [Get("/api/DayOffExclusions/{id}")]
        Task<DayOffExclusionContract> Get(Guid id);

        /// <summary>
        /// Get all compiled exclusions
        /// </summary>
        [Get("/api/DayOffExclusions/compiled")]
        Task<CompiledExclusionsListContract> ListCompiled();

        /// <summary>
        /// Create exclusion
        /// </summary> 
        [Post("/api/DayOffExclusions")]
        Task<DayOffExclusionContract> Create([Body] DayOffExclusionContract contract);

        /// <summary>
        /// Update exclusion
        /// </summary>
        [Put("/api/DayOffExclusions")]
        Task<DayOffExclusionContract> Update([Body] DayOffExclusionContract contract);

        /// <summary>
        /// Delete exclusion
        /// </summary>
        [Delete("/api/DayOffExclusions/{id}")]
        Task Delete(Guid id);
    }
}