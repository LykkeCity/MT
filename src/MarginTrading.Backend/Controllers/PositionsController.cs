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
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Filters;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Middleware;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoreLinq;

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
        public async Task CloseAsync([CanBeNull] [FromRoute] string positionId,
            [FromBody] PositionCloseRequest request = null)
        {
            if (!_ordersCache.Positions.TryGetPositionById(positionId, out var position))
            {
                throw new InvalidOperationException("Position not found");
            }

            ValidateDayOff(position.AssetPairId);

            var originator = GetOriginator(request?.Originator);

            var correlationId = request?.CorrelationId ?? _identityGenerator.GenerateGuid();

            var order = await _tradingEngine.ClosePositionAsync(positionId, originator, request?.AdditionalInfo,
                correlationId, request?.Comment);

            if (order.Status != OrderStatus.Executed && order.Status != OrderStatus.ExecutionStarted)
            {
                throw new InvalidOperationException(order.RejectReasonText);
            }

            _operationsLogService.AddLog("action order.close", order.AccountId, request?.ToJson(),
                order.ToJson());
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
        public Task CloseGroupAsync([FromQuery] string assetPairId = null, [FromQuery] string accountId = null, 
            [FromQuery] PositionDirectionContract? direction = null, [FromBody] PositionCloseRequest request = null)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new ArgumentNullException(nameof(accountId), "AccountId must be set.");
            }

            var operationId = string.IsNullOrWhiteSpace(request?.CorrelationId) 
                ? _identityGenerator.GenerateGuid()
                : request.CorrelationId;

            _cqrsSender.SendCommandToSelf(new StartLiquidationInternalCommand
            {
                OperationId = operationId,
                CreationTime = _dateService.Now(),
                AccountId = accountId,
                AssetPairId = assetPairId,
                Direction = direction?.ToType<PositionDirection>(),
                QuoteInfo = null,
                LiquidationType = LiquidationType.Forced,
            });
            
            _operationsLogService.AddLog("Position liquidation started", string.Empty, 
                $"instrument = [{assetPairId}], account = [{accountId}], direction = [{direction}], request = [{request.ToJson()}]",
                $"Started operation {operationId}");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get a position by id
        /// </summary>
        [HttpGet, Route("{positionId}")]
        public Task<OpenPositionContract> GetAsync(string positionId)
        {
            if (!_ordersCache.Positions.TryGetPositionById(positionId, out var order))
                return null;

            return Task.FromResult(Convert(order));
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

            return Task.FromResult(positions.Select(Convert).ToList());
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
                contents: filtered.Select(Convert).ToList(),
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

        internal static OpenPositionContract Convert(Position position)
        {
            return new OpenPositionContract
            {
                AccountId = position.AccountId,
                AssetPairId = position.AssetPairId,
                CurrentVolume = position.Volume,
                Direction = position.Direction.ToType<PositionDirectionContract>(),
                Id = position.Id,
                OpenPrice = position.OpenPrice,
                OpenFxPrice = position.OpenFxPrice,
                ClosePrice = position.ClosePrice,
                ExpectedOpenPrice = position.ExpectedOpenPrice,
                OpenTradeId = position.OpenTradeId,
                OpenOrderType = position.OpenOrderType.ToType<OrderTypeContract>(),
                OpenOrderVolume = position.OpenOrderVolume,
                PnL = position.GetFpl(),
                ChargedPnl = position.ChargedPnL,
                Margin = position.GetMarginMaintenance(),
                FxRate = position.GetFplRate(),
                FxAssetPairId = position.FxAssetPairId,
                FxToAssetPairDirection = position.FxToAssetPairDirection.ToType<FxToAssetPairDirectionContract>(),
                RelatedOrders = position.RelatedOrders.Select(o => o.Id).ToList(),
                RelatedOrderInfos = position.RelatedOrders.Select(o =>
                    new RelatedOrderInfoContract {Id = o.Id, Type = o.Type.ToType<OrderTypeContract>()}).ToList(),
                OpenTimestamp = position.OpenDate,
                ModifiedTimestamp = position.LastModified,
                TradeId = position.Id
            };
        }
        
        private OriginatorType GetOriginator(OriginatorTypeContract? originator)
        {
            if (originator == null || originator.Value == default(OriginatorTypeContract))
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
