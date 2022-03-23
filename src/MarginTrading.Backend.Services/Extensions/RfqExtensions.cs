// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Rfq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Rfq;
using MarginTrading.Backend.Services.Services;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.Extensions
{
    public static class RfqExtensions
    {
        public static Rfq ToRfq(this OperationExecutionInfoWithPause<SpecialLiquidationOperationData> o)
        {
            return new Rfq
            {
                Id = o.Id,
                InstrumentId = o.Data.Instrument,
                PositionIds = o.Data.PositionIds,
                Volume = o.Data.Volume,
                Price = o.Data.Price,
                ExternalProviderId = o.Data.ExternalProviderId,
                AccountId = o.Data.AccountId,
                CausationOperationId = o.Data.CausationOperationId,
                CreatedBy = string.IsNullOrEmpty(o.Data.AdditionalInfo)
                    ? null
                    : JsonConvert.DeserializeObject<RfqAdditionalInfo>(o.Data.AdditionalInfo)?.CreatedBy,
                OriginatorType = o.Data.OriginatorType,
                RequestNumber = o.Data.RequestNumber,
                RequestedFromCorporateActions = o.Data.RequestedFromCorporateActions,
                State = o.Data.State,
                LastModified = o.LastModified,
                PauseSummary = IRfqPauseService.CalculatePauseSummary(o)
            };
        }

        public static RfqChangedEvent ToEventContract(this OperationExecutionInfoWithPause<SpecialLiquidationOperationData> o)
        {
            return new RfqChangedEvent
            {
                Id = o.Id,
                PositionIds = o.Data.PositionIds,
                Volume = o.Data.Volume,
                Price = o.Data.Price,
                RequestNumber = o.Data.RequestNumber,
                State = (RfqOperationState) o.Data.State,
                LastModified = o.LastModified,
                AccountId = o.Data.AccountId,
                InstrumentId = o.Data.Instrument,
                PauseSummary = IRfqPauseService.CalculatePauseSummary(o).ToEventContract()
            };
        }

        private static RfqPauseSummaryChangedContract ToEventContract(this RfqPauseSummary o)
        {
            return new RfqPauseSummaryChangedContract
            {
                CanBePaused = o.CanBePaused,
                CanBeResumed = o.CanBeResumed,
                IsPaused = o.IsPaused,
                PauseReason = o.PauseReason,
                ResumeReason = o.ResumeReason
            };
        }
    }
}