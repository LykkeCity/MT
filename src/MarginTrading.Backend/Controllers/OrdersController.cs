using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.AzureRepositories.Snow.OrdersById;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Helpers;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Mappers;
using MarginTrading.Common.Extensions;
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
        private readonly IOperationsLogService _operationsLogService;
        private readonly IConsole _consoleWriter;
        private readonly OrdersCache _ordersCache;
        private readonly IAssetPairDayOffService _assetDayOffService;
        private readonly IDateService _dateService;
        private readonly IValidateOrderService _validateOrderService;
        private readonly IIdentityGenerator _identityGenerator;

        public OrdersController(IAssetPairsCache assetPairsCache, ITradingEngine tradingEngine,
            IAccountsCacheService accountsCacheService, IOperationsLogService operationsLogService,
            IConsole consoleWriter, OrdersCache ordersCache, IAssetPairDayOffService assetDayOffService,
            IDateService dateService, IValidateOrderService validateOrderService, IIdentityGenerator identityGenerator)
        {
            _assetPairsCache = assetPairsCache;
            _tradingEngine = tradingEngine;
            _accountsCacheService = accountsCacheService;
            _operationsLogService = operationsLogService;
            _consoleWriter = consoleWriter;
            _ordersCache = ordersCache;
            _assetDayOffService = assetDayOffService;
            _dateService = dateService;
            _validateOrderService = validateOrderService;
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
            var orders = await _validateOrderService.ValidateRequestAndGetOrders(request); 
            
            var placedOrder = await _tradingEngine.PlaceOrderAsync(orders.order);

            _consoleWriter.WriteLine($"Order place. Account: [{request.AccountId}], Order: [{placedOrder.Id}]");
            
            _operationsLogService.AddLog("action order.place", request.AccountId, request.ToJson(),
                placedOrder.ToJson());

            if (placedOrder.Status == OrderStatus.Rejected)
            {
                throw new Exception($"Order is rejected: {placedOrder.RejectReason} ({placedOrder.RejectReasonText})");
            }
            else
            {
                foreach (var order in orders.relatedOrders)
                {
                    var placedRelatedOrder = await _tradingEngine.PlaceOrderAsync(order);
                    
                    _consoleWriter.WriteLine(
                        $"Related order place. Account: [{request.AccountId}], Order: [{placedRelatedOrder.Id}]");
            
                    _operationsLogService.AddLog("action related.order.place", request.AccountId, request.ToJson(),
                        placedRelatedOrder.ToJson());
                }
            }
        }

        /// <summary>
        /// Cancel existiong order
        /// </summary>
        /// <param name="orderId">Id of order to cancel</param>
        /// <param name="request">Additional cancellation info</param>
        [Route("{orderId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpDelete]
        public Task CancelAsync(string orderId, [FromBody] OrderCancelRequest request = null)
        {
            if (!_ordersCache.TryGetOrderById(orderId, out var order))
                throw new InvalidOperationException("Order not found");

            var originator = GetOriginator(request?.Originator);

            var correlationId = string.IsNullOrWhiteSpace(request?.CorrelationId)
                ? _identityGenerator.GenerateGuid()
                : request.CorrelationId;

            var canceledOrder = _tradingEngine.CancelPendingOrder(order.Id, originator, request?.AdditionalInfo, 
                correlationId, request?.Comment);

            _consoleWriter.WriteLine($"action order.cancel for accountId = {order.AccountId}, orderId = {orderId}");
            _operationsLogService.AddLog("action order.cancel", order.AccountId, request?.ToJson(),
                canceledOrder.ToJson());

            return Task.CompletedTask;
        }

        /// <summary>
        /// Change existing order
        /// </summary>
        /// <param name="orderId">Id of order to change</param>
        /// <param name="request">Values to change</param>
        /// <exception cref="InvalidOperationException"></exception>
        [Route("{orderId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpPut]
        public Task ChangeAsync(string orderId, [FromBody] OrderChangeRequest request)
        {
            if (!_ordersCache.TryGetOrderById(orderId, out var order))
                throw new InvalidOperationException("Order not found");

            try
            {
                var originator = GetOriginator(request.Originator);

                var correlationId = string.IsNullOrWhiteSpace(request?.CorrelationId)
                    ? _identityGenerator.GenerateGuid()
                    : request.CorrelationId;

                _tradingEngine.ChangeOrderLimits(order.Id, request.Price, originator, request.AdditionalInfo, 
                    correlationId);
            }
            catch (ValidateOrderException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }

            _consoleWriter.WriteLine($"action order.changeLimits for orderId = {orderId}");
            _operationsLogService.AddLog("action order.changeLimits", order.AccountId, request.ToJson(), "");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Get order by id 
        /// </summary>
        [HttpGet, Route("{orderId}")]
        public Task<OrderContract> GetAsync(string orderId)
        {
            return _ordersCache.TryGetOrderById(orderId, out var order)
                ? Task.FromResult(order.ConvertToContract())
                : Task.FromResult<OrderContract>(null);
        }

        /// <summary>
        /// Get open orders with optional filtering
        /// </summary>
        [HttpGet, Route("")]
        public Task<List<OrderContract>> ListAsync([FromQuery] string accountId = null,
            [FromQuery] string assetPairId = null, [FromQuery] string parentPositionId = null,
            [FromQuery] string parentOrderId = null)
        {
            // do not call get by account, it's slower for single account 
            IEnumerable<Order> orders = _ordersCache.GetAllOrders();

            if (!string.IsNullOrWhiteSpace(accountId))
                orders = orders.Where(o => o.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(assetPairId))
                orders = orders.Where(o => o.AssetPairId == assetPairId);

            if (!string.IsNullOrWhiteSpace(parentPositionId))
                orders = orders.Where(o => o.ParentPositionId == parentPositionId);

            if (!string.IsNullOrWhiteSpace(parentOrderId))
                orders = orders.Where(o => o.ParentOrderId == parentOrderId);

            return Task.FromResult(orders.Select(o => o.ConvertToContract()).ToList());
        }

        /// <summary>
        /// Get open orders with optional filtering and pagination
        /// </summary>
        [HttpGet, Route("by-pages")]
        public Task<PaginatedResponseContract<OrderContract>> ListAsyncByPages(
            [FromQuery] string accountId = null,
            [FromQuery] string assetPairId = null, [FromQuery] string parentPositionId = null,
            [FromQuery] string parentOrderId = null,
            [FromQuery] int? skip = null, [FromQuery] int? take = null)
        {
            if ((skip.HasValue && !take.HasValue) || (!skip.HasValue && take.HasValue))
            {
                throw new ArgumentOutOfRangeException(nameof(skip), "Both skip and take must be set or unset");
            }

            if (take.HasValue && (take <= 0 || skip < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be >= 0, take must be > 0");
            }
            
            var orders = _ordersCache.GetAllOrders().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(accountId))
                orders = orders.Where(o => o.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(assetPairId))
                orders = orders.Where(o => o.AssetPairId == assetPairId);

            if (!string.IsNullOrWhiteSpace(parentPositionId))
                orders = orders.Where(o => o.ParentPositionId == parentPositionId);

            if (!string.IsNullOrWhiteSpace(parentOrderId))
                orders = orders.Where(o => o.ParentOrderId == parentOrderId);

            var orderList = orders.OrderByDescending(x => x.Created).ToList();
            var filtered = (take == null ? orderList : orderList.Skip(skip.Value))
                .Take(PaginationHelper.GetTake(take)).ToList();

            return Task.FromResult(new PaginatedResponseContract<OrderContract>(
                contents: filtered.Select(o => o.ConvertToContract()).ToList(),
                start: skip ?? 0,
                size: filtered.Count,
                totalSize: orderList.Count
            ));
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