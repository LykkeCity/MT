using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.TradingConditions;
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
        public async Task CloseAsync([FromRoute] string positionId, [FromBody] PositionCloseRequest request)
        {
            if (!_ordersCache.ActiveOrders.TryGetOrderById(positionId, out var order))
            {
                throw new InvalidOperationException("Position not found");
            }

            if (_assetDayOffService.IsDayOff(order.Instrument))
            {
                throw new InvalidOperationException("Trades for instrument are not available");
            }

            var reason =
                request.Originator == OriginatorTypeContract.OnBehalf ||
                request.Originator == OriginatorTypeContract.System
                    ? OrderCloseReason.ClosedByBroker
                    : OrderCloseReason.Close;

            order = await _tradingEngine.CloseActiveOrderAsync(positionId, reason, request.Comment);

            if (order.Status != OrderStatus.Closed && order.Status != OrderStatus.Closing)
            {
                throw new InvalidOperationException(order.CloseRejectReasonText);
            }

            _consoleWriter.WriteLine(
                $"action position.close, orderId = {positionId}");
            _operationsLogService.AddLog("action order.close", order.AccountId, request.ToJson(),
                order.ToJson());
        }

        /// <summary>
        /// Close group of opened positions by itrument and direction
        /// </summary>
        /// <param name="instrumentId">Positions instrument</param>
        /// <param name="direction">Positions direction (Long or Short), optional</param>
        /// <param name="request">Additional info for close</param>
        [Route("instrument-group/{instrumentId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpDelete]
        public Task CloseGroupAsync([FromRoute] string instrumentId,
            [FromBody] PositionCloseRequest request, [FromQuery] PositionDirectionContract? direction = null)
        {
            throw new NotImplementedException();
        }
    }
}
