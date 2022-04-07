// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.Rfq;
using MarginTrading.Backend.Core.Rfq;

namespace MarginTrading.Backend.Extensions
{
    public static class RfqExtensions
    {
        public static RfqFilter ToFilter(this ListRfqRequest request)
        {
            return request != null
                ? new RfqFilter
                {
                    AccountId = request.AccountId,
                    InstrumentId = request.InstrumentId,
                    OperationId = request.RfqId,
                    DateFrom = request.DateFrom,
                    DateTo = request.DateTo,
                    States = request.States,
                    CanBePaused = request.CanBePaused,
                    CanBeResumed = request.CanBeResumed,
                }
                : null;
        }

        public static RfqContract ToContract(this RfqWithPauseSummary rfqWithPauseSummary)
        {
            return new RfqContract
            {
                Id = rfqWithPauseSummary.Id,
                InstrumentId = rfqWithPauseSummary.InstrumentId,
                PositionIds = rfqWithPauseSummary.PositionIds,
                Volume = rfqWithPauseSummary.Volume,
                Price = rfqWithPauseSummary.Price,
                ExternalProviderId = rfqWithPauseSummary.ExternalProviderId,
                AccountId = rfqWithPauseSummary.AccountId,
                CausationOperationId = rfqWithPauseSummary.CausationOperationId,
                CreatedBy = rfqWithPauseSummary.CreatedBy,
                OriginatorType = (RfqOriginatorType)rfqWithPauseSummary.OriginatorType,
                RequestNumber = rfqWithPauseSummary.RequestNumber,
                RequestedFromCorporateActions = rfqWithPauseSummary.RequestedFromCorporateActions,
                State = (RfqOperationState)rfqWithPauseSummary.State,
                LastModified = rfqWithPauseSummary.LastModified,
                Pause = rfqWithPauseSummary.PauseSummary != null
                    ? new RfqPauseSummaryContract
                    {
                        CanBePaused = rfqWithPauseSummary.PauseSummary.CanBePaused,
                        CanBeResumed = rfqWithPauseSummary.PauseSummary.CanBeResumed,
                        CanBeStopped = rfqWithPauseSummary.PauseSummary.CanBeStopped,
                        IsPaused = rfqWithPauseSummary.PauseSummary.IsPaused,
                        PauseReason = rfqWithPauseSummary.PauseSummary.PauseReason,
                        ResumeReason = rfqWithPauseSummary.PauseSummary.ResumeReason
                    }
                    : null
            };
        }
    }
}