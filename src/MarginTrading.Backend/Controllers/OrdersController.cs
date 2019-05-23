using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.TradeMonitoring;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Helpers;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Filters;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Infrastructure;
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
        private readonly ILog _log;
        private readonly OrdersCache _ordersCache;
        private readonly IAssetPairDayOffService _assetDayOffService;
        private readonly IDateService _dateService;
        private readonly IValidateOrderService _validateOrderService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ICqrsSender _cqrsSender;

        public OrdersController(IAssetPairsCache assetPairsCache, 
            ITradingEngine tradingEngine,
            IAccountsCacheService accountsCacheService, 
            IOperationsLogService operationsLogService,
            ILog log, 
            OrdersCache ordersCache, 
            IAssetPairDayOffService assetDayOffService,
            IDateService dateService, 
            IValidateOrderService validateOrderService, 
            IIdentityGenerator identityGenerator,
            ICqrsSender cqrsSender)
        {
            _assetPairsCache = assetPairsCache;
            _tradingEngine = tradingEngine;
            _accountsCacheService = accountsCacheService;
            _operationsLogService = operationsLogService;
            _log = log;
            _ordersCache = ordersCache;
            _assetDayOffService = assetDayOffService;
            _dateService = dateService;
            _validateOrderService = validateOrderService;
            _identityGenerator = identityGenerator;
            _cqrsSender = cqrsSender;
        }

        /// <summary>
        /// Place new order
        /// </summary>
        /// <param name="request">Order model</param>
        /// <returns>Order Id</returns>
        [Route("")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [ServiceFilter(typeof(MarginTradingEnabledFilter))]
        [HttpPost]
        public async Task<string> PlaceAsync([FromBody] OrderPlaceRequest request)
        {
            var (baseOrder, relatedOrders) = (default(Order), default(List<Order>));
            try
            {
                (baseOrder, relatedOrders) = await _validateOrderService.ValidateRequestAndCreateOrders(request);
            }
            catch (ValidateOrderException exception)
            {
                _cqrsSender.PublishEvent(new OrderPlacementRejectedEvent
                {
                    CorrelationId = request.CorrelationId ?? _identityGenerator.GenerateGuid(),
                    EventTimestamp = _dateService.Now(),
                    OrderPlaceRequest = request,
                    RejectReason = exception.RejectReason.ToType<OrderRejectReasonContract>(),
                    RejectReasonText = exception.Message,
                });
                throw;
            }

            var placedOrder = await _tradingEngine.PlaceOrderAsync(baseOrder);
            
            _operationsLogService.AddLog("action order.place", request.AccountId, request.ToJson(),
                placedOrder.ToJson());

            if (placedOrder.Status == OrderStatus.Rejected)
            {
                throw new Exception($"Order is rejected: {placedOrder.RejectReason} ({placedOrder.RejectReasonText})");
            }

            foreach (var order in relatedOrders)
            {
                var placedRelatedOrder = await _tradingEngine.PlaceOrderAsync(order);

                _operationsLogService.AddLog("action related.order.place", request.AccountId, request.ToJson(),
                    placedRelatedOrder.ToJson());
            }

            return placedOrder.Id;
        }

        /// <summary>
        /// Cancel existing order
        /// </summary>
        /// <param name="orderId">Id of order to cancel</param>
        /// <param name="request">Additional cancellation info</param>
        [Route("{orderId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [ServiceFilter(typeof(MarginTradingEnabledFilter))]
        [HttpDelete]
        public Task CancelAsync(string orderId, [FromBody] OrderCancelRequest request = null)
        {
            if (!_ordersCache.TryGetOrderById(orderId, out var order))
                throw new InvalidOperationException("Order not found");

            var correlationId = string.IsNullOrWhiteSpace(request?.CorrelationId)
                ? _identityGenerator.GenerateGuid()
                : request.CorrelationId;

            var reason = request?.Originator == OriginatorTypeContract.System
                ? OrderCancellationReason.CorporateAction
                : OrderCancellationReason.None;
            
            var canceledOrder = _tradingEngine.CancelPendingOrder(order.Id, request?.AdditionalInfo, 
                correlationId, request?.Comment, reason);

            _operationsLogService.AddLog("action order.cancel", order.AccountId, request?.ToJson(),
                canceledOrder.ToJson());

            return Task.CompletedTask;
        }

        /// <summary>
        /// Close group of orders by accountId, assetPairId and direction.
        /// </summary>
        /// <param name="accountId">Mandatory</param>
        /// <param name="assetPairId">Optional</param>
        /// <param name="direction">Optional</param>
        /// <param name="request">Optional</param>
        /// <returns>Dictionary of failed to close orderIds with exception message</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        [Route("close-group")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [ServiceFilter(typeof(MarginTradingEnabledFilter))]
        [HttpDelete]
        public async Task<Dictionary<string, string>> CloseGroupAsync([FromQuery] string accountId, 
            [FromQuery] string assetPairId = null,
            [FromQuery] OrderDirectionContract? direction = null,
            [FromBody] OrderCancelRequest request = null)
        {
            accountId.RequiredNotNullOrWhiteSpace(nameof(accountId));
            
            var failedOrderIds = new Dictionary<string, string>();

            foreach (var order in _ordersCache.GetPending()
                .Where(x => x.AccountId == accountId
                            && (string.IsNullOrEmpty(assetPairId) || x.AssetPairId == assetPairId)
                            && (direction == null || x.Direction == direction.ToType<OrderDirection>())))
            {
                try
                {
                    await CancelAsync(order.Id, request);
                }
                catch (Exception exception)
                {
                    await _log.WriteWarningAsync(nameof(OrdersController), nameof(CloseGroupAsync),
                        "Failed to cancel order [{order.Id}]", exception);
                    failedOrderIds.Add(order.Id, exception.Message);
                }
            }

            return failedOrderIds;
        }

        /// <summary>
        /// Change existing order
        /// </summary>
        /// <param name="orderId">Id of order to change</param>
        /// <param name="request">Values to change</param>
        /// <exception cref="InvalidOperationException"></exception>
        [Route("{orderId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [ServiceFilter(typeof(MarginTradingEnabledFilter))]
        [HttpPut]
        public Task ChangeAsync(string orderId, [FromBody] OrderChangeRequest request)
        {
            if (!_ordersCache.TryGetOrderById(orderId, out var order))
            {
                throw new InvalidOperationException("Order not found");
            }

            try
            {
                var originator = GetOriginator(request.Originator);

                var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
                    ? _identityGenerator.GenerateGuid()
                    : request.CorrelationId;

                _tradingEngine.ChangeOrder(order.Id, request.Price, request.Validity, originator,
                    request.AdditionalInfo, correlationId, request.ForceOpen);
            }
            catch (ValidateOrderException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }

            _operationsLogService.AddLog("action order.changeLimits", order.AccountId, 
                new { orderId = orderId, request = request.ToJson() }.ToJson(), "");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Get order by id 
        /// </summary>
        [HttpGet, Route("{orderId}")]
        public Task<OrderContract> GetAsync(string orderId)
        {
            return _ordersCache.TryGetOrderById(orderId, out var order)
                ? Task.FromResult(order.ConvertToContract(_ordersCache))
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

            return Task.FromResult(orders.Select(o => o.ConvertToContract(_ordersCache)).ToList());
        }

        /// <summary>
        /// Get open orders with optional filtering and pagination. Sorted descending by default.
        /// </summary>
        [HttpGet, Route("by-pages")]
        public Task<PaginatedResponseContract<OrderContract>> ListAsyncByPages(
            [FromQuery] string accountId = null,
            [FromQuery] string assetPairId = null, [FromQuery] string parentPositionId = null,
            [FromQuery] string parentOrderId = null,
            [FromQuery] int? skip = null, [FromQuery] int? take = null,
            [FromQuery] string order = LykkeConstants.DescendingOrder)
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

            var orderList = (order == LykkeConstants.AscendingOrder
                    ? orders.OrderBy(x => x.Created)
                    : orders.OrderByDescending(x => x.Created))
                .ToList();
            var filtered = (take == null ? orderList : orderList.Skip(skip.Value))
                .Take(PaginationHelper.GetTake(take)).ToList();

            return Task.FromResult(new PaginatedResponseContract<OrderContract>(
                contents: filtered.Select(o => o.ConvertToContract(_ordersCache)).ToList(),
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