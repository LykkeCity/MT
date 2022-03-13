// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.Rfq;
using MarginTrading.Backend.Core.Rfq;

namespace MarginTrading.Backend.Extensions
{
    public static class RfqExtensions
    {
        public static RfqFilter ToFilter(this GetRfqRequest request)
        {
            return request != null
                ? new RfqFilter
                {
                    AccountId = request.AccountId,
                    InstrumentId = request.InstrumentId,
                    OperationId = request.RfqId,
                    DateFrom = request.DateFrom,
                    DateTo = request.DateTo,
                    States = request.States
                }
                : null;
        }

        public static RfqContract ToContract(this Rfq rfq)
        {
            return new RfqContract
            {
                Id = rfq.Id,
                InstrumentId = rfq.InstrumentId,
                PositionIds = rfq.PositionIds,
                Volume = rfq.Volume,
                Price = rfq.Price,
                ExternalProviderId = rfq.ExternalProviderId,
                AccountId = rfq.AccountId,
                CausationOperationId = rfq.CausationOperationId,
                CreatedBy = rfq.CreatedBy,
                OriginatorType = (RfqOriginatorType)rfq.OriginatorType,
                RequestNumber = rfq.RequestNumber,
                RequestedFromCorporateActions = rfq.RequestedFromCorporateActions,
                State = (RfqOperationState)rfq.State,
                LastModified = rfq.LastModified,
                Pause = rfq.PauseSummary != null
                    ? new RfqPauseSummaryContract
                    {
                        CanBePaused = rfq.PauseSummary.CanBePaused,
                        CanBeResumed = rfq.PauseSummary.CanBeResumed,
                        IsPaused = rfq.PauseSummary.IsPaused,
                        PauseReason = rfq.PauseSummary.PauseReason,
                        ResumeReason = rfq.PauseSummary.ResumeReason
                    }
                    : null
            };
        }
    }
}