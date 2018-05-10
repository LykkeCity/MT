using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Common.Middleware;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/orders")]
    public class OrdersController : Controller, IOrdersApi
    {
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly ITradingEngine _tradingEngine;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IMarginTradingOperationsLogService _operationsLogService;
        private readonly IConsole _consoleWriter;
        private readonly OrdersCache _ordersCache;
        private readonly IAssetPairDayOffService _assetDayOffService;
        private readonly IIdentityGenerator _identityGenerator;

        public OrdersController(
            IAssetPairsCache assetPairsCache,
            ITradingEngine tradingEngine,
            IAccountsCacheService accountsCacheService,
            IMarginTradingOperationsLogService operationsLogService,
            IConsole consoleWriter,
            OrdersCache ordersCache,
            IAssetPairDayOffService assetDayOffService,
            IIdentityGenerator identityGenerator)
        {
            _assetPairsCache = assetPairsCache;
            _tradingEngine = tradingEngine;
            _accountsCacheService = accountsCacheService;
            _operationsLogService = operationsLogService;
            _consoleWriter = consoleWriter;
            _ordersCache = ordersCache;
            _assetDayOffService = assetDayOffService;
            _identityGenerator = identityGenerator;
        }
        
        /// <summary>
        /// Place new order
        /// </summary>
        /// <param name="request">Order model</param>
        [Route("")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpPost]
        public async Task PlaceAsync([FromBody] OrderPlaceRequest request)
        {
            var code = await _identityGenerator.GenerateIdAsync(nameof(Order));

            var order = new Order
            {
                Id = Guid.NewGuid().ToString("N"),
                Code = code,
                CreateDate = DateTime.UtcNow,
                //TODO: remove ClientId
                ClientId = request.AccountId,
                AccountId = request.AccountId,
                Instrument = request.InstrumentId,
                Volume = request.Direction == OrderDirectionContract.Buy ? request.Volume : -request.Volume,
                ExpectedOpenPrice = request.Price
            };

            var placedOrder = await _tradingEngine.PlaceOrderAsync(order);

            _consoleWriter.WriteLine($"action order.place for accountId = {request.AccountId}");
            _operationsLogService.AddLog("action order.place", request.AccountId, request.AccountId, request.ToJson(),
                placedOrder.ToJson());
        }

        /// <summary>
        /// Cancel existiong order
        /// </summary>
        /// <param name="orderId">Id of order to cancel</param>
        /// <param name="request">Additional cancellation info</param>
        [Route("{orderId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpDelete]
        public Task CancelAsync(string orderId, [FromBody] OrderCancelRequest request)
        {
            if (!_ordersCache.WaitingForExecutionOrders.TryGetOrderById(orderId, out var order))
            {
                throw new InvalidOperationException("Order not found");
            }

            if (_assetDayOffService.IsDayOff(order.Instrument))
            {
                throw new InvalidOperationException("Trades for instrument are not available");
            }

            var reason =
                request.Originator == OriginatorTypeContract.OnBehalf ||
                request.Originator == OriginatorTypeContract.System
                    ? OrderCloseReason.CanceledByBroker
                    : OrderCloseReason.Canceled;

            var canceledOrder = _tradingEngine.CancelPendingOrder(order.Id, reason, request.Comment);

            _consoleWriter.WriteLine(
                $"action order.cancel for accountId = {order.AccountId}, orderId = {orderId}");
            _operationsLogService.AddLog("action order.cancel", order.ClientId, order.AccountId, request.ToJson(),
                canceledOrder.ToJson());
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Change existion order
        /// </summary>
        /// <param name="orderId">Id of order to change</param>
        /// <param name="request">Values to change</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [Route("{orderId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpPut]
        public Task ChangeAsync(string orderId, [FromBody]OrderChangeRequest request)
        {
            if (!_ordersCache.TryGetOrderById(orderId, out var order))
            {
                throw new InvalidOperationException("Order not found");
            }
            
            if (_assetDayOffService.IsDayOff(order.Instrument))
            {
                throw new InvalidOperationException("Trades for instrument are not available");
            }

            try
            {
                _tradingEngine.ChangeOrderLimits(orderId, null, null, request.Price);
            }
            catch (ValidateOrderException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }

            _consoleWriter.WriteLine($"action order.changeLimits for orderId = {orderId}");
            _operationsLogService.AddLog("action order.changeLimits", order.ClientId, order.AccountId, request.ToJson(),
                "");

            return Task.CompletedTask;
        }
    }
}
