using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
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
        private readonly IMarginTradingOperationsLogService _operationsLogService;
        private readonly IConsole _consoleWriter;
        private readonly OrdersCache _ordersCache;
        private readonly IAssetPairDayOffService _assetDayOffService;

        public PositionsController(
            ITradingEngine tradingEngine,
            IMarginTradingOperationsLogService operationsLogService,
            IConsole consoleWriter,
            OrdersCache ordersCache,
            IAssetPairDayOffService assetDayOffService)
        {
            _tradingEngine = tradingEngine;
            _operationsLogService = operationsLogService;
            _consoleWriter = consoleWriter;
            _ordersCache = ordersCache;
            _assetDayOffService = assetDayOffService;
        }
        
        /// <summary>
        /// Close opened position
        /// </summary>
        /// <param name="positionId">Id of position</param>
        /// <param name="request">Additional info for close</param>
        [Route("{positionId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpDelete]
        public async Task CloseAsync([CanBeNull] [FromRoute] string positionId/*, [FromBody] PositionCloseRequest request*/)
        {
            if (!_ordersCache.Positions.TryGetOrderById(positionId, out var position))
            {
                throw new InvalidOperationException("Position not found");
            }

            //if (_assetDayOffService.IsDayOff(position.AssetPairId))
            //{
            //    throw new InvalidOperationException("Trades for instrument are not available");
            //}

            var reason = PositionCloseReason.Close;
//                request.Originator == OriginatorTypeContract.OnBehalf ||
//                request.Originator == OriginatorTypeContract.System
//                    ? OrderCloseReason.ClosedByBroker
//                    : OrderCloseReason.Close;

            var order = await _tradingEngine.ClosePositionAsync(positionId, reason, /*request.Comment*/ "");

            if (order.Status != OrderStatus.Executed && order.Status != OrderStatus.ExecutionStarted)
            {
                throw new InvalidOperationException(order.RejectReasonText);
            }

            _consoleWriter.WriteLine(
                $"action position.close, orderId = {positionId}");
            _operationsLogService.AddLog("action order.close", order.AccountId, ""/*request.ToJson()*/,
                order.ToJson());
        }

        /// <summary>
        /// Close group of opened positions by instrument and direction (optional)
        /// </summary>
        /// <param name="instrument">Positions instrument</param>
        /// <param name="direction">Positions direction (Long or Short), optional</param>
        [Route("instrument-group/{instrument}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpDelete]
        public async Task CloseGroupAsync([FromRoute] string instrument,
            /*[FromBody] PositionCloseRequest request,*/
            [FromQuery] PositionDirectionContract? direction = null)
        {
            var positions = _ordersCache.Positions.GetAllOrders();
            
            if (!string.IsNullOrWhiteSpace(instrument))
                positions = positions.Where(o => o.AssetPairId == instrument).ToList();

            if (direction != null)
            {
                var positionDirection = direction.ToType<PositionDirection>();

                positions = positions.Where(o => o.Direction == positionDirection).ToList();
            }

            var reason = PositionCloseReason.Close;
//                request.Originator == OriginatorTypeContract.OnBehalf ||
//                request.Originator == OriginatorTypeContract.System
//                    ? OrderCloseReason.ClosedByBroker
//                    : OrderCloseReason.Close;
            
            foreach (var orderId in positions.Select(o => o.Id).ToList())
            {
                var closedOrder = await _tradingEngine.ClosePositionAsync(orderId, reason, /*request.Comment*/ "");

                if (closedOrder.Status != OrderStatus.Executed && closedOrder.Status != OrderStatus.ExecutionStarted)
                {
                    throw new InvalidOperationException(closedOrder.RejectReasonText);
                }

                _operationsLogService.AddLog("action close positions group", closedOrder.AccountId, ""/* request.ToJson()*/,
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
        [Route("account-group/{accountId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpDelete]
        public async Task CloseGroupAsync([FromRoute] string accountId, [FromQuery] string assetPairId = null)
        {
            var orders = _ordersCache.Positions.GetAllOrders();

            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            orders = orders.Where(o => o.AccountId == accountId
                                       && (string.IsNullOrWhiteSpace(assetPairId) || o.AssetPairId == assetPairId))
                .ToList();

            var reason = PositionCloseReason.Close;
//                request.Originator == OriginatorTypeContract.OnBehalf ||
//                request.Originator == OriginatorTypeContract.System
//                    ? OrderCloseReason.ClosedByBroker
//                    : OrderCloseReason.Close;
            
            foreach (var orderId in orders.Select(o => o.Id).ToList())
            {
                var closedOrder = await _tradingEngine.ClosePositionAsync(orderId, reason, /*request.Comment*/ "");

                if (closedOrder.Status != OrderStatus.Executed && closedOrder.Status != OrderStatus.ExecutionStarted)
                {
                    throw new InvalidOperationException(closedOrder.RejectReasonText);
                }

                _operationsLogService.AddLog("action close positions group", closedOrder.AccountId, ""/* request.ToJson()*/,
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
            IEnumerable<Position> orders = _ordersCache.Positions.GetAllOrders();
            if (!string.IsNullOrWhiteSpace(accountId))
                orders = orders.Where(o => o.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(assetPairId))
                orders = orders.Where(o => o.AssetPairId == assetPairId);

            return Task.FromResult(orders.Select(Convert).ToList());
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
                ClosePrice = position.ClosePrice,
                ExpectedOpenPrice = position.ExpectedOpenPrice,
                PnL = position.GetFpl(),
                Margin = position.GetMarginMaintenance(),
                FxRate = position.GetFplRate(),
                RelatedOrders = position.RelatedOrders.Select(o => o.Id).ToList(),
                RelatedOrderInfos = position.RelatedOrders.Select(o =>
                    new RelatedOrderInfoContract {Id = o.Id, Type = o.Type.ToType<OrderTypeContract>()}).ToList(),
                OpenTimestamp = position.OpenDate,
                TradeId = position.Id
            };
        }
    }
}
