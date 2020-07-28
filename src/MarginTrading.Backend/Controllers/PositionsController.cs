// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Helpers;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Filters;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Mappers;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Middleware;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/positions")]
    public class PositionsController : Controller, IPositionsApi
    {
        private readonly ITradingEngine _tradingEngine;
        private readonly IOperationsLogService _operationsLogService;
        private readonly ILog _log;
        private readonly OrdersCache _ordersCache;
        private readonly IAssetPairDayOffService _assetDayOffService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ICqrsSender _cqrsSender;
        private readonly IDateService _dateService;

        public PositionsController(
            ITradingEngine tradingEngine,
            IOperationsLogService operationsLogService,
            ILog log,
            OrdersCache ordersCache,
            IAssetPairDayOffService assetDayOffService,
            IIdentityGenerator identityGenerator,
            ICqrsSender cqrsSender,
            IDateService dateService)
        {
            _tradingEngine = tradingEngine;
            _operationsLogService = operationsLogService;
            _log = log;
            _ordersCache = ordersCache;
            _assetDayOffService = assetDayOffService;
            _identityGenerator = identityGenerator;
            _cqrsSender = cqrsSender;
            _dateService = dateService;
        }

        /// <summary>
        /// Close opened position
        /// </summary>
        /// <param name="positionId">Id of position</param>
        /// <param name="request">Additional info for close</param>
        [Route("{positionId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [ServiceFilter(typeof(MarginTradingEnabledFilter))]
        [HttpDelete]
        public async Task<PositionCloseResponse> CloseAsync([CanBeNull] [FromRoute] string positionId,
            [FromBody] PositionCloseRequest request = null)
        {
            if (!_ordersCache.Positions.TryGetPositionById(positionId, out var position))
            {
                throw new InvalidOperationException("Position not found");
            }

            ValidateDayOff(position.AssetPairId);

            var originator = GetOriginator(request?.Originator);

            var correlationId = request?.CorrelationId ?? _identityGenerator.GenerateGuid();

            var closeResult = await _tradingEngine.ClosePositionsAsync(
                new PositionsCloseData(
                    new List<Position> {position},
                    position.AccountId,
                    position.AssetPairId,
                    position.OpenMatchingEngineId,
                    position.ExternalProviderId,
                    originator,
                    request?.AdditionalInfo,
                    correlationId,
                    position.EquivalentAsset), true);

            _operationsLogService.AddLog("action order.close", position.AccountId, request?.ToJson(),
                closeResult.ToJson());

            return new PositionCloseResponse
            {
                PositionId = positionId,
                OrderId = closeResult.order?.Id,
                Result = closeResult.result.ToType<PositionCloseResultContract>()
            };
        }

        /// <summary>
        /// Close group of opened positions by accountId, assetPairId and direction.
        /// AccountId must be passed. Method signature allow nulls for backward compatibility.
        /// </summary>
        /// <param name="assetPairId">Optional</param>
        /// <param name="accountId">Mandatory</param>
        /// <param name="direction">Optional</param>
        /// <param name="request">Optional</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        [Route("close-group")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [ServiceFilter(typeof(MarginTradingEnabledFilter))]
        [HttpDelete]
        public async Task<PositionsGroupCloseResponse> CloseGroupAsync([FromQuery] string assetPairId = null, 
        [FromQuery] string accountId = null, [FromQuery] PositionDirectionContract? direction = null, [FromBody] PositionCloseRequest request = null)
        {
            var originator = GetOriginator(request?.Originator);

            var closeResult = await _tradingEngine.ClosePositionsGroupAsync(accountId, assetPairId, direction?.ToType<PositionDirection>(), originator, request?.AdditionalInfo, request?.CorrelationId);
            
            _operationsLogService.AddLog("Position liquidation started", string.Empty, 
                $"instrument = [{assetPairId}], account = [{accountId}], direction = [{direction}], request = [{request.ToJson()}]",
                closeResult?.ToJson());

            return new PositionsGroupCloseResponse
            {
                Responses = closeResult.Select(r => new PositionCloseResponse
                {
                    PositionId = r.Key,
                    Result = r.Value.Item1.ToType<PositionCloseResultContract>(),
                    OrderId = r.Value.Item2?.Id
                }).ToArray()
            };
        }

        /// <summary>
        /// Get a position by id
        /// </summary>
        [HttpGet, Route("{positionId}")]
        public Task<OpenPositionContract> GetAsync(string positionId)
        {
            if (!_ordersCache.Positions.TryGetPositionById(positionId, out var position))
                return null;

            return Task.FromResult(position.ConvertToContract(_ordersCache));
        }

        /// <summary>
        /// Get open positions 
        /// </summary>
        [HttpGet, Route("")]
        public Task<List<OpenPositionContract>> ListAsync([FromQuery]string accountId = null,
            [FromQuery] string assetPairId = null)
        {
            var positions = _ordersCache.Positions.GetAllPositions().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(accountId))
                positions = positions.Where(o => o.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(assetPairId))
                positions = positions.Where(o => o.AssetPairId == assetPairId);

            return Task.FromResult(positions.Select(x => x.ConvertToContract(_ordersCache)).ToList());
        }

        /// <summary>
        /// Get positions with optional filtering and pagination
        /// </summary>
        [HttpGet, Route("by-pages")]
        public Task<PaginatedResponseContract<OpenPositionContract>> ListAsyncByPages(string accountId = null,
            string assetPairId = null, int? skip = null, int? take = null)
        {
            if ((skip.HasValue && !take.HasValue) || (!skip.HasValue && take.HasValue))
            {
                throw new ArgumentOutOfRangeException(nameof(skip), "Both skip and take must be set or unset");
            }

            if (take.HasValue && (take <= 0 || skip < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be >= 0, take must be > 0");
            }
            
            var positions = _ordersCache.Positions.GetAllPositions().AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(accountId))
                positions = positions.Where(o => o.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(assetPairId))
                positions = positions.Where(o => o.AssetPairId == assetPairId);

            var positionList = positions.OrderByDescending(x => x.OpenDate).ToList();
            var filtered = (take == null ? positionList : positionList.Skip(skip.Value))
                .Take(PaginationHelper.GetTake(take)).ToList();

            return Task.FromResult(new PaginatedResponseContract<OpenPositionContract>(
                contents: filtered.Select(x => x.ConvertToContract(_ordersCache)).ToList(),
                start: skip ?? 0,
                size: filtered.Count,
                totalSize: positionList.Count
            ));
        }

        /// <summary>
        /// FOR TEST PURPOSES ONLY!
        /// </summary>
        [HttpPost, Route("special-liquidation")]
        public void StartSpecialLiquidation(string[] positionIds, [CanBeNull] string accountId)
        {
            _cqrsSender.SendCommandToSelf(new StartSpecialLiquidationInternalCommand
            {
                OperationId = _identityGenerator.GenerateGuid(),
                CreationTime = _dateService.Now(),
                PositionIds = positionIds,
                AccountId = accountId,
            });
        }

        private OriginatorType GetOriginator(OriginatorTypeContract? originator)
        {
            if (originator == null || originator.Value == default)
            {
                return OriginatorType.Investor;
            }

            return originator.ToType<OriginatorType>();
        }

        private void ValidateDayOff(params string[] assetPairIds)
        {
            foreach (var instrument in assetPairIds)
            {
                if (_assetDayOffService.IsDayOff(instrument))
                {
                    throw new InvalidOperationException($"Trades for {instrument} are not available");
                }
            }
        }
    }
}
