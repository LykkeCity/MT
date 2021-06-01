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

        public RfqController(IOperationExecutionInfoRepository operationExecutionInfoRepository)
        {
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
        }

        [HttpGet]
        public async Task<PaginatedResponseContract<RfqContract>> GetAsync([CanBeNull, FromQuery] GetRfqRequest getRfqRequest, [FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            var states = getRfqRequest.States?.Select(x => (SpecialLiquidationOperationState)x).ToList();
            var data =  await _operationExecutionInfoRepository.GetRfqAsync(getRfqRequest.RfqId, getRfqRequest.InstrumentId, getRfqRequest.AccountId, states, getRfqRequest.DateFrom, getRfqRequest.DateTo, skip, take);

            var rfq = data.Contents.Select(Convert).ToList();
            return new PaginatedResponseContract<RfqContract>(rfq, skip, data.Contents.Count, data.TotalSize);
        }

        private RfqContract Convert(OperationExecutionInfo<SpecialLiquidationOperationData> operation)
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
            };
        }
    }
}