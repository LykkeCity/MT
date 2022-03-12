// Copyright (c) 2021 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.Rfq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Core.Rfq;
using MarginTrading.Backend.Services.Services;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/rfq")]
    public class RfqController : Controller, IRfqApi
    {
        private class AdditionalInfo
        {
            public string CreatedBy { get; set; }
        }

        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly IRfqPauseService _rfqPauseService;

        public RfqController(IOperationExecutionInfoRepository operationExecutionInfoRepository, IRfqPauseService rfqPauseService)
        {
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _rfqPauseService = rfqPauseService;
        }

        [HttpGet]
        public async Task<PaginatedResponseContract<RfqContract>> GetAsync([CanBeNull, FromQuery] GetRfqRequest getRfqRequest, [FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            var states = getRfqRequest.States?
                .Select(x => (SpecialLiquidationOperationState)x)
                .ToList();

            var data = await _operationExecutionInfoRepository
                .GetRfqAsync(getRfqRequest.RfqId,
                    getRfqRequest.InstrumentId,
                    getRfqRequest.AccountId,
                    states,
                    getRfqRequest.DateFrom,
                    getRfqRequest.DateTo,
                    skip,
                    take);

            var rfq = data
                .Contents
                .Select(Convert)
                .ToList();
            
            return new PaginatedResponseContract<RfqContract>(rfq, skip, data.Contents.Count, data.TotalSize);
        }

        [HttpPost]
        [Route("{id}/pause")]
        public async Task<RfqPauseErrorCode> PauseAsync(string id, [FromBody] RfqPauseRequest request)
        {
            var errorCode = await _rfqPauseService.AddAsync(id, PauseSource.Manual, request.Initiator);

            return errorCode;
        }

        [HttpGet]
        [Route("{id}/pause")]
        public async Task<RfqPauseInfoContract> GetPauseInfoAsync(string id)
        {
            var pause = await _rfqPauseService.GetCurrentAsync(id);

            if (pause == null)
                return null;

            return new RfqPauseInfoContract
            {
                State = pause?.State.ToString(),
                CreatedAt = pause.CreatedAt,
                EffectiveSince = pause.EffectiveSince,
                Initiator = pause.Initiator,
                Source = pause.Source.ToString()
            };
        }

        [HttpPost]
        [Route("{id}/resume")]
        public async Task<RfqResumeErrorCode> ResumeAsync(string id, [FromBody] RfqResumeRequest request)
        {
            var errorCode = await _rfqPauseService.ResumeAsync(id, PauseCancellationSource.Manual, request.Initiator);

            return errorCode;
        }

        private RfqContract Convert(OperationExecutionInfoWithPause<SpecialLiquidationOperationData> operation)
        {
            return new RfqContract
            {
                Id = operation.Id,
                InstrumentId = operation.Data.Instrument,
                PositionIds = operation.Data.PositionIds,
                Volume = operation.Data.Volume,
                Price = operation.Data.Price,
                ExternalProviderId = operation.Data.ExternalProviderId,
                AccountId = operation.Data.AccountId,
                CausationOperationId = operation.Data.CausationOperationId,
                CreatedBy = string.IsNullOrEmpty(operation.Data.AdditionalInfo) ? null : JsonConvert.DeserializeObject<AdditionalInfo>(operation.Data.AdditionalInfo).CreatedBy,
                OriginatorType = (RfqOriginatorType)operation.Data.OriginatorType,
                RequestNumber = operation.Data.RequestNumber,
                RequestedFromCorporateActions = operation.Data.RequestedFromCorporateActions,
                State = (RfqOperationState)operation.Data.State,
                LastModified = operation.LastModified,
                Pause = new RfqPauseDetailsContract
                {
                    CanBePaused = (operation.Pause == null || operation.Pause.State == PauseState.Cancelled) && RfqPauseService.AllowedOperationStatesToPauseIn.Contains(operation.Data.State),
                    CanBeResumed = operation.Pause?.State == PauseState.Active,
                    IsPaused = operation.Pause?.State == PauseState.Active || operation.Pause?.State == PauseState.PendingCancellation,
                    PauseReason = operation.Pause?.Source.ToString(),
                    // todo: currently, we'll never get this value cause only not cancelled pauses are taken into account
                    // and only cancelled pauses have information on resume reason
                    ResumeReason = operation.Pause?.CancellationSource?.ToString()
                }
            };
        }
    }
}