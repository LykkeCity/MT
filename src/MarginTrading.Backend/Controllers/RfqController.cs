// Copyright (c) 2021 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.Rfq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Core.Rfq;
using MarginTrading.Backend.Extensions;
using MarginTrading.Backend.Services.Services;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/rfq")]
    public class RfqController : Controller, IRfqApi
    {
        private readonly IRfqService _rfqService;
        private readonly IRfqPauseService _rfqPauseService;

        public RfqController(IRfqPauseService rfqPauseService, IRfqService rfqService)
        {
            _rfqPauseService = rfqPauseService;
            _rfqService = rfqService;
        }

        [HttpGet]
        public async Task<PaginatedResponseContract<RfqContract>> GetAsync(
            [FromQuery] GetRfqRequest getRfqRequest,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20)
        {
            var result = await _rfqService.GetAsync(getRfqRequest.ToFilter(), skip, take);

            return new PaginatedResponseContract<RfqContract>(
                result.Contents.Select(rfq => rfq.ToContract()).ToList(),
                skip,
                result.Contents.Count,
                result.TotalSize);
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
    }
}