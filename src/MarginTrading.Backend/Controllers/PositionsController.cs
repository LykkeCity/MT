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
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
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
        private readonly IConsole _consoleWriter;
        private readonly OrdersCache _ordersCache;
        private readonly IAssetPairDayOffService _assetDayOffService;
        private readonly IIdentityGenerator _identityGenerator;

        public PositionsController(
            ITradingEngine tradingEngine,
            IOperationsLogService operationsLogService,
            IConsole consoleWriter,
            OrdersCache ordersCache,
            IAssetPairDayOffService assetDayOffService,
            IIdentityGenerator identityGenerator)
        {
            _tradingEngine = tradingEngine;
            _operationsLogService = operationsLogService;
            _consoleWriter = consoleWriter;
            _ordersCache = ordersCache;
            _assetDayOffService = assetDayOffService;
            _identityGenerator = identityGenerator;
        }

        /// <summary>
        /// Close opened position
        /// </summary>
        /// <param name="positionId">Id of position</param>
        /// <param name="request">Additional info for close</param>
        [Route("{positionId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpDelete]
        public async Task CloseAsync([CanBeNull] [FromRoute] string positionId,
            [FromBody] PositionCloseRequest request = null)
        {
            if (!_ordersCache.Positions.TryGetOrderById(positionId, out var position))
            {
                throw new InvalidOperationException("Position not found");
            }

            //if (_assetDayOffService.IsDayOff(position.AssetPairId))
            //{
            //    throw new InvalidOperationException("Trades for instrument are not available");
            //}

            var originator = GetOriginator(request?.Originator);

            var correlationId = request?.CorrelationId ?? _identityGenerator.GenerateGuid();

            var order = await _tradingEngine.ClosePositionAsync(positionId, originator, request?.AdditionalInfo,
                correlationId, request?.Comment);

            if (order.Status != OrderStatus.Executed && order.Status != OrderStatus.ExecutionStarted)
            {
                throw new InvalidOperationException(order.RejectReasonText);
            }

            _consoleWriter.WriteLine(
                $"action position.close, orderId = {positionId}");
            _operationsLogService.AddLog("action order.close", order.AccountId, request?.ToJson(),
                order.ToJson());
        }

        /// <summary>
        /// Close group of opened positions optionally by assetPairId, accountId and direction.
        /// AssetPairId or AccountId must be passed.
        /// </summary>
        /// <param name="assetPairId"></param>
        /// <param name="accountId"></param>
        /// <param name="direction"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        [Route("close-group")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpDelete]
        public async Task CloseGroupAsync([FromQuery] string assetPairId = null, [FromQuery] string accountId = null, 
            [FromQuery] PositionDirectionContract? direction = null, [FromBody] PositionCloseRequest request = null)
        {
            if (string.IsNullOrWhiteSpace(assetPairId) && string.IsNullOrWhiteSpace(accountId))
            {
                throw new ArgumentNullException(nameof(assetPairId), "AssetPairId or accountId must be set.");
            }

            var positions = _ordersCache.Positions.GetAllOrders()
                .Where(x => (string.IsNullOrWhiteSpace(assetPairId) || x.AssetPairId == assetPairId)
                            && (string.IsNullOrWhiteSpace(accountId) || x.AccountId == accountId)
                            && (direction == null || x.Direction == direction.ToType<PositionDirection>()))
                .ToList();
            
            var originator = GetOriginator(request?.Originator);
            
            var correlationId = request?.CorrelationId ?? _identityGenerator.GenerateGuid();
            
            foreach (var orderId in positions.Select(o => o.Id).ToList())
            {
                var closedOrder =
                    await _tradingEngine.ClosePositionAsync(orderId, originator, request?.AdditionalInfo, 
                        correlationId, request?.Comment);

                if (closedOrder.Status != OrderStatus.Executed && closedOrder.Status != OrderStatus.ExecutionStarted)
                {
                    throw new InvalidOperationException(closedOrder.RejectReasonText);
                }

                _operationsLogService.AddLog("action close positions group", closedOrder.AccountId, request?.ToJson(),
                    orderId);
            }

            _consoleWriter.WriteLine(
                $"action close positions group. instrument = [{assetPairId}], account = [{accountId}], direction = [{direction}]");
        }

        /// <summary>
        /// Close group of opened positions by instrument and direction (optional)
        /// </summary>
        /// <param name="instrument">Positions instrument</param>
        /// <param name="request">Additional info for close</param>
        /// <param name="direction">Positions direction (Long or Short), optional</param>
        [Route("instrument-group/{instrument}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpDelete]
        [Obsolete("Will be removed soon. Use close-group with instrument, account and direction.")]
        public async Task CloseGroupAsync([FromRoute] string instrument,
            [FromQuery] PositionDirectionContract? direction = null,
            [FromBody] PositionCloseRequest request = null)
        {
            var positions = _ordersCache.Positions.GetAllOrders();
            
            if (!string.IsNullOrWhiteSpace(instrument))
                positions = positions.Where(o => o.AssetPairId == instrument).ToList();

            if (direction != null)
            {
                var positionDirection = direction.ToType<PositionDirection>();

                positions = positions.Where(o => o.Direction == positionDirection).ToList();
            }

            var originator = GetOriginator(request?.Originator);
            
            var correlationId = request?.CorrelationId ?? _identityGenerator.GenerateGuid();
            
            foreach (var orderId in positions.Select(o => o.Id).ToList())
            {
                var closedOrder =
                    await _tradingEngine.ClosePositionAsync(orderId, originator, request?.AdditionalInfo, 
                        correlationId, request?.Comment);

                if (closedOrder.Status != OrderStatus.Executed && closedOrder.Status != OrderStatus.ExecutionStarted)
                {
                    throw new InvalidOperationException(closedOrder.RejectReasonText);
                }

                _operationsLogService.AddLog("action close positions group", closedOrder.AccountId, request?.ToJson(),
                    orderId);
            }
            
            _consoleWriter.WriteLine(
                $"action close positions group. instrument = [{instrument}], direction = [{direction}]");
        }

        /// <summary>
        /// Close group of opened positions by account and instrument (optional)
        /// </summary>
        /// <param name="accountId">Account id</param>
        /// <param name="assetPairId">Instrument id, optional</param>
        /// <param name="request">Additional info for close</param>
        [Route("account-group/{accountId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpDelete]
        [Obsolete("Will be removed soon. Use close-group with instrument, account and direction.")]
        public async Task CloseGroupAsync([FromRoute] string accountId, [FromQuery] string assetPairId = null, 
            [FromBody] PositionCloseRequest request = null)
        {
            var orders = _ordersCache.Positions.GetAllOrders();

            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            orders = orders.Where(o => o.AccountId == accountId && 
                                       (string.IsNullOrWhiteSpace(assetPairId) || o.AssetPairId == assetPairId)).ToList();

            var originator = GetOriginator(request?.Originator);
            
            var correlationId = request?.CorrelationId ?? _identityGenerator.GenerateGuid();
            
            foreach (var orderId in orders.Select(o => o.Id).ToList())
            {
                var closedOrder =
                    await _tradingEngine.ClosePositionAsync(orderId, originator, request?.AdditionalInfo, 
                        correlationId, request?.Comment);

                if (closedOrder.Status != OrderStatus.Executed && closedOrder.Status != OrderStatus.ExecutionStarted)
                {
                    throw new InvalidOperationException(closedOrder.RejectReasonText);
                }

                _operationsLogService.AddLog("action close positions group", closedOrder.AccountId, request?.ToJson(),
                    orderId);
            }
            
            _consoleWriter.WriteLine(
                $"action close positions group. account = [{accountId}], assetPair = [{assetPairId}]");
        }

        /// <summary>
        /// Get a position by id
        /// </summary>
        [HttpGet, Route("{positionId}")]
        public Task<OpenPositionContract> GetAsync(string positionId)
        {
            if (!_ordersCache.Positions.TryGetOrderById(positionId, out var order))
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
            var positions = _ordersCache.Positions.GetAllOrders().AsEnumerable();
            
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
            
            var positions = _ordersCache.Positions.GetAllOrders().AsEnumerable();
            
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

        private OpenPositionContract Convert(Position position)
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
                PnL = position.GetFpl(),
                ChargedPnl = position.ChargedPnL,
                Margin = position.GetMarginMaintenance(),
                FxRate = position.GetFplRate(),
                RelatedOrders = position.RelatedOrders.Select(o => o.Id).ToList(),
                RelatedOrderInfos = position.RelatedOrders.Select(o =>
                    new RelatedOrderInfoContract {Id = o.Id, Type = o.Type.ToType<OrderTypeContract>()}).ToList(),
                OpenTimestamp = position.OpenDate,
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
    }
}
