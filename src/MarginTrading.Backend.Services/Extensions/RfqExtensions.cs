// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
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
        public static RfqWithPauseSummary ToRfqWithPauseSummary(this OperationExecutionInfoWithPause<SpecialLiquidationOperationData> o)
        {
            return new RfqWithPauseSummary
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
                    : Deserialize(o.Data.AdditionalInfo)?.CreatedBy,
                OriginatorType = o.Data.OriginatorType,
                RequestNumber = o.Data.RequestNumber,
                RequestedFromCorporateActions = o.Data.RequestedFromCorporateActions,
                State = o.Data.State,
                LastModified = o.LastModified,
                PauseSummary = IRfqPauseService.CalculatePauseSummary(o)
            };
        }

        public static RfqEvent ToEventContract(this OperationExecutionInfoWithPause<SpecialLiquidationOperationData> o, RfqEventTypeContract eventType, string brokerId) =>
            new RfqEvent
            {
                EventType = eventType,
                RfqSnapshot = new RfqContract
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
                        : Deserialize(o.Data.AdditionalInfo)?.CreatedBy,
                    OriginatorType = (RfqOriginatorType)o.Data.OriginatorType,
                    RequestNumber = o.Data.RequestNumber,
                    RequestedFromCorporateActions = o.Data.RequestedFromCorporateActions,
                    State = (RfqOperationState)o.Data.State,
                    LastModified = o.LastModified,
                    Pause = IRfqPauseService.CalculatePauseSummary(o).ToEventContract()
                },
                BrokerId = brokerId
            };

        public static List<SpecialLiquidationOperationState> MapStates(this RfqFilter filter) =>
            filter?
                .States?
                .Select(x => (SpecialLiquidationOperationState)x)
                .ToList();

        public static Func<RfqWithPauseSummary, bool> GetApplyPauseFilterFunc(this RfqFilter filter)
        {
            return o =>
            {
                if (filter == null)
                    return true;

                if (!filter.CanBePaused.HasValue &&
                    !filter.CanBeResumed.HasValue &&
                    !filter.CanBeStopped.HasValue
                   )
                {
                    return true;
                }

                return (filter.CanBePaused.HasValue && o.PauseSummary.CanBePaused == filter.CanBePaused) ||
                       (filter.CanBeResumed.HasValue && o.PauseSummary.CanBeResumed == filter.CanBeResumed) ||
                       (filter.CanBeStopped.HasValue && o.PauseSummary.CanBeStopped == filter.CanBeStopped);
            };
        }

        private static RfqPauseSummaryContract ToEventContract(this RfqPauseSummary o) =>
            new RfqPauseSummaryContract
            {
                CanBePaused = o.CanBePaused,
                CanBeResumed = o.CanBeResumed,
                IsPaused = o.IsPaused,
                PauseReason = o.PauseReason,
                ResumeReason = o.ResumeReason
            };

        private static RfqAdditionalInfo Deserialize(string source)
        {
            try
            {
                return JsonConvert.DeserializeObject<RfqAdditionalInfo>(source);
            }
            catch (JsonReaderException)
            {
                return null;
            }
        }
    }
}