// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Rfq;

namespace MarginTrading.Backend.Services.Services
{
    public interface IRfqPauseService
    {
        /// <summary>
        /// Add new pause for the operation
        /// </summary>
        /// <param name="operationId"></param>
        /// <param name="source"></param>
        /// <param name="initiator"></param>
        /// <returns></returns>
        Task<RfqPauseErrorCode> AddAsync(string operationId, PauseSource source, Initiator initiator);

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
        Task<bool> AcknowledgeAsync(string operationId);

        /// <summary>
        /// If there is pending cancellation pause - cancel it 
        /// </summary>
        /// <param name="operationId"></param>
        /// <returns></returns>
        Task<bool> AcknowledgeCancellationAsync(string operationId);

        /// <summary>
        /// Resumes paused special liquidation 
        /// </summary>
        /// <param name="operationId"></param>
        /// <param name="source"></param>
        /// <param name="initiator"></param>
        /// <returns></returns>
        Task<RfqResumeErrorCode> ResumeAsync(string operationId, PauseCancellationSource source, Initiator initiator);

        /// <summary>
        /// Stop pending pause
        /// </summary>
        /// <param name="operationId"></param>
        /// <param name="source"></param>
        /// <param name="initiator"></param>
        /// <returns></returns>
        Task StopPendingAsync(string operationId, PauseCancellationSource source, Initiator initiator);

        /// <summary>
        /// Builds pause summary for operation
        /// </summary>
        /// <param name="operationExecutionInfo"></param>
        /// <returns></returns>
        static RfqPauseSummary GetPauseSummary(
            OperationExecutionInfoWithPause<SpecialLiquidationOperationData> operationExecutionInfo)
        {
            return new RfqPauseSummary
            {
                CanBePaused = (operationExecutionInfo.Pause == null || operationExecutionInfo.Pause.State == PauseState.Cancelled) &&
                              RfqPauseService.AllowedOperationStatesToPauseIn.Contains(operationExecutionInfo.Data.State),
                CanBeResumed = operationExecutionInfo.Pause?.State == PauseState.Active,
                IsPaused = operationExecutionInfo.Pause?.State == PauseState.Active ||
                           operationExecutionInfo.Pause?.State == PauseState.PendingCancellation,
                PauseReason = operationExecutionInfo.Pause?.Source.ToString(),
                // todo: currently, we'll never get this value cause only not cancelled pauses are taken into account
                // and only cancelled pauses have information on resume reason
                ResumeReason = operationExecutionInfo.Pause?.CancellationSource?.ToString()
            };
        }
    }
}