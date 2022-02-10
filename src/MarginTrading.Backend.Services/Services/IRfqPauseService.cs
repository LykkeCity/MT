// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Core.Rfq;

namespace MarginTrading.Backend.Services.Services
{
    public interface IRfqPauseService
    {
        /// <summary>
        /// Add new pause for the operation
        /// </summary>
        /// <param name="operationId"></param>
        /// <param name="initiator"></param>
        /// <returns></returns>
        Task<RfqPauseErrorCode> AddAsync(string operationId, Initiator initiator);

        /// <summary>
        /// Get current pending or active pause object. Cancelled pauses are not taken into account
        /// </summary>
        /// <param name="operationId"></param>
        /// <returns></returns>
        Task<Pause> GetCurrentAsync(string operationId);

        /// <summary>
        /// If there is pending pause - activate it
        /// </summary>
        /// <param name="operationId"></param>
        /// <returns></returns>
        Task<bool> AcknowledgeIfPausedAsync(string operationId);

        Task ContinueAsync(string operationId);
    }
}