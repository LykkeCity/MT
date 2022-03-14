// Copyright (c) 2021 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.Rfq;
using Refit;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.ErrorCodes;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// API for performing operations with RFQ
    /// </summary>
    [PublicAPI]
    public interface IRfqApi
    {
        /// <summary>
        /// Returns RFQs
        /// </summary>
        [Get("/api/rfq")]
        Task<PaginatedResponseContract<RfqContract>> GetAsync([Query, CanBeNull] ListRfqRequest listRfqRequest, [Query] int skip = 0, [Query] int take = 20);

        /// <summary>
        /// Pauses RFQ workflow
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [Post("/api/rfq/{id}/pause")]
        Task<RfqPauseErrorCode> PauseAsync(string id, [Body] RfqPauseRequest request);
        
        /// <summary>
        /// Get information on pause 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Get("/api/rfq/{id}/pause")]
        Task<RfqPauseInfoContract> GetPauseInfoAsync(string id);

        /// <summary>
        /// Resumes RFQ workflow
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [Post("/api/rfq/{id}/resume")]
        Task<RfqResumeErrorCode> ResumeAsync(string id, [Body] RfqResumeRequest request);
    }
}