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
            if (!_ordersCache.ActiveOrders.TryGetOrderById(positionId, out var order))
            {
                throw new InvalidOperationException("Position not found");
            }

            if (_assetDayOffService.IsDayOff(order.Instrument))
            {
                throw new InvalidOperationException("Trades for instrument are not available");
            }

            var reason = OrderCloseReason.Close;
//                request.Originator == OriginatorTypeContract.OnBehalf ||
//                request.Originator == OriginatorTypeContract.System
//                    ? OrderCloseReason.ClosedByBroker
//                    : OrderCloseReason.Close;

            order = await _tradingEngine.CloseActiveOrderAsync(positionId, reason, /*request.Comment*/ "");

            if (order.Status != PositionStatus.Closed && order.Status != PositionStatus.Closing)
            {
                throw new InvalidOperationException(order.CloseRejectReasonText);
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
            var orders = _ordersCache.ActiveOrders.GetAllOrders();
            
            if (!string.IsNullOrWhiteSpace(instrument))
                orders = orders.Where(o => o.Instrument == instrument).ToList();

            if (direction != null)
            {
                var orderDirection = direction == PositionDirectionContract.Short
                    ? OrderDirection.Sell
                    : OrderDirection.Buy;

                orders = orders.Where(o => o.GetOrderDirection() == orderDirection).ToList();
            }

            var reason = OrderCloseReason.Close;
//                request.Originator == OriginatorTypeContract.OnBehalf ||
//                request.Originator == OriginatorTypeContract.System
//                    ? OrderCloseReason.ClosedByBroker
//                    : OrderCloseReason.Close;
            
            foreach (var orderId in orders.Select(o => o.Id).ToList())
            {
                var closedOrder = await _tradingEngine.CloseActiveOrderAsync(orderId, reason, /*request.Comment*/ "");

                if (closedOrder.Status != PositionStatus.Closed && closedOrder.Status != PositionStatus.Closing)
                {
                    throw new InvalidOperationException(closedOrder.CloseRejectReasonText);
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
            var orders = _ordersCache.ActiveOrders.GetAllOrders();

            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            orders = orders.Where(o => o.AccountId == accountId
                                       && (string.IsNullOrWhiteSpace(assetPairId) || o.Instrument == assetPairId))
                .ToList();

            var reason = OrderCloseReason.Close;
//                request.Originator == OriginatorTypeContract.OnBehalf ||
//                request.Originator == OriginatorTypeContract.System
//                    ? OrderCloseReason.ClosedByBroker
//                    : OrderCloseReason.Close;
            
            foreach (var orderId in orders.Select(o => o.Id).ToList())
            {
                var closedOrder = await _tradingEngine.CloseActiveOrderAsync(orderId, reason, /*request.Comment*/ "");

                if (closedOrder.Status != PositionStatus.Closed && closedOrder.Status != PositionStatus.Closing)
                {
                    throw new InvalidOperationException(closedOrder.CloseRejectReasonText);
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
            if (!_ordersCache.ActiveOrders.TryGetOrderById(positionId, out var order))
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
            IEnumerable<Position> orders = _ordersCache.ActiveOrders.GetAllOrders();
            if (!string.IsNullOrWhiteSpace(accountId))
                orders = orders.Where(o => o.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(assetPairId))
                orders = orders.Where(o => o.Instrument == assetPairId);

            return Task.FromResult(orders.Select(Convert).ToList());
        }

        private OpenPositionContract Convert(Position order)
        {
            var relatedOrders = new List<string>();
            
            if (order.StopLoss != null)
            {
                relatedOrders.Add($"{order.Id}_{OrderTypeContract.StopLoss.ToString()}");
            }
            if (order.TakeProfit != null)
            {
                relatedOrders.Add($"{order.Id}_{OrderTypeContract.TakeProfit.ToString()}");
            }
            
            return new OpenPositionContract
            {
                AccountId = order.AccountId,
                AssetPairId = order.Instrument,
                CurrentVolume = order.Volume,
                Direction = Convert(order.GetOrderDirection()),
                Id = order.Id,
                OpenPrice = order.OpenPrice,
                ClosePrice = order.ClosePrice,
                ExpectedOpenPrice = order.ExpectedOpenPrice,
                PnL = order.GetFpl(),
                Margin = order.GetMarginMaintenance(),
                FxRate = order.GetFplRate(),
                RelatedOrders = relatedOrders,
                OpenTimestamp = order.OpenDate.RequiredNotNull(nameof(order.OpenDate)),
                TradeId = order.Id + '_' + order.GetOrderDirection(),
            };
        }

        private PositionDirectionContract Convert(OrderDirection orderOpenType)
        {
            switch (orderOpenType)
            {
                case OrderDirection.Buy:
                    return PositionDirectionContract.Long;
                case OrderDirection.Sell:
                    return PositionDirectionContract.Short;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderOpenType), orderOpenType, null);
            }
        }
    }
}
