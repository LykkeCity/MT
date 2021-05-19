// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
using MarginTrading.Backend.Services.Helpers;
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
        private readonly ITradingEngine _tradingEngine;
        private readonly IOperationsLogService _operationsLogService;
        private readonly ILog _log;
        private readonly OrdersCache _ordersCache;
        private readonly IDateService _dateService;
        private readonly IValidateOrderService _validateOrderService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ICqrsSender _cqrsSender;

        public OrdersController(
            ITradingEngine tradingEngine,
            IOperationsLogService operationsLogService,
            ILog log,
            OrdersCache ordersCache,
            IDateService dateService,
            IValidateOrderService validateOrderService,
            IIdentityGenerator identityGenerator,
            ICqrsSender cqrsSender)
        {
            _tradingEngine = tradingEngine;
            _operationsLogService = operationsLogService;
            _log = log;
            _ordersCache = ordersCache;
            _dateService = dateService;
            _validateOrderService = validateOrderService;
            _identityGenerator = identityGenerator;
            _cqrsSender = cqrsSender;
        }

        /// <summary>
        /// Update related order bulk
        /// </summary>
        [Route("bulk")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [ServiceFilter(typeof(MarginTradingEnabledFilter))]
        [HttpPatch]
        public async Task<Dictionary<string, string>> UpdateRelatedOrderBulkAsync([FromBody] UpdateRelatedOrderBulkRequest request)
        {
            var result = new Dictionary<string, string>();

            foreach (var id in request.PositionIds)
            {
                try
                {
                    await UpdateRelatedOrderAsync(id, request.UpdateRelatedOrderRequest);
                }
                catch (Exception exception)
                {
                    await _log.WriteWarningAsync(nameof(OrdersController), nameof(UpdateRelatedOrderBulkAsync),
                        $"Failed to update related order for position {id}", exception);
                    result.Add(id, exception.Message);
                }
            }

            return result;
        }

        /// <summary>
        /// Update related order
        /// </summary>
        [Route("{positionId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [ServiceFilter(typeof(MarginTradingEnabledFilter))]
        [HttpPatch]
        public async Task UpdateRelatedOrderAsync(string positionId, [FromBody] UpdateRelatedOrderRequest request)
        {
            if (!_ordersCache.Positions.TryGetPositionById(positionId, out var position))
                throw new InvalidOperationException($"Position {positionId} not found");

            ValidationHelper.ValidateAccountId(position, request.AccountId); 

            var takeProfit = position.RelatedOrders?.FirstOrDefault(x => x.Type == OrderType.TakeProfit);
            var stopLoss = position.RelatedOrders?.FirstOrDefault(x => x.Type == OrderType.StopLoss || x.Type == OrderType.TrailingStop);

            var relatedOrderShouldBeRemoved = request.NewPrice == default;
            var relatedOrderId = request.OrderType == RelatedOrderTypeContract.TakeProfit ? takeProfit?.Id : stopLoss?.Id;
            var relatedOrderExists = !string.IsNullOrWhiteSpace(relatedOrderId);

            if (!relatedOrderShouldBeRemoved)
            {
                if (request.OrderType == RelatedOrderTypeContract.StopLoss && relatedOrderExists)
                {
                    var order = _ordersCache.GetOrderById(relatedOrderId);
                    if ((order.OrderType == OrderType.TrailingStop) != request.HasTrailingStop)
                    {
                        await CancelAsync(relatedOrderId, new OrderCancelRequest
                        {
                            Originator = request.Originator,
                            AdditionalInfo = request.AdditionalInfoJson
                        }, request.AccountId);
                        relatedOrderExists = false;
                    }
                }
                if (!relatedOrderExists)
                {
                    var orderPlaceRequest = new OrderPlaceRequest
                    {
                        AccountId = request.AccountId,
                        InstrumentId = position.AssetPairId,
                        Direction = position.Direction == PositionDirection.Long ? OrderDirectionContract.Buy : OrderDirectionContract.Sell,
                        Price = request.NewPrice,
                        Volume = position.Volume,
                        Type = request.OrderType == RelatedOrderTypeContract.TakeProfit ? OrderTypeContract.TakeProfit : OrderTypeContract.StopLoss,
                        Originator = request.Originator,
                        ForceOpen = false,
                        PositionId = position.Id,
                        AdditionalInfo = request.AdditionalInfoJson,
                        UseTrailingStop = request.HasTrailingStop ?? false
                    };

                    if (orderPlaceRequest.UseTrailingStop
                        && orderPlaceRequest.Type == OrderTypeContract.StopLoss
                        && (!string.IsNullOrWhiteSpace(orderPlaceRequest.ParentOrderId) || !string.IsNullOrWhiteSpace(orderPlaceRequest.PositionId)))
                    {
                        orderPlaceRequest.Type = OrderTypeContract.TrailingStop;
                        orderPlaceRequest.UseTrailingStop = false;
                    }

                    await PlaceAsync(orderPlaceRequest);
                }
                else
                {
                    await ChangeAsync(relatedOrderId, new OrderChangeRequest
                    {
                        Price = request.NewPrice,
                        Originator = request.Originator,
                        AdditionalInfo = request.AdditionalInfoJson,
                        AccountId = request.AccountId,
                    });
                }
            }
            else if (relatedOrderExists)
            {
                await CancelAsync(relatedOrderId, 
                    new OrderCancelRequest
                    {
                        Originator = request.Originator,
                        AdditionalInfo = request.AdditionalInfoJson
                    },
                    request.AccountId);
            }
            else
            {
                throw new Exception($"Couldn't update related order for position {positionId}");
            }
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
                var message = $"Order {placedOrder.Id} from account {placedOrder.AccountId} for instrument {placedOrder.AssetPairId} is rejected: {placedOrder.RejectReason} ({placedOrder.RejectReasonText}). Comment: {placedOrder.Comment}.";
                switch (placedOrder.RejectReason)
                {
                    case OrderRejectReason.NotEnoughBalance:
                    case OrderRejectReason.InvalidInstrument:
                        throw new ValidateOrderFunctionalException(placedOrder.RejectReason, placedOrder.RejectReasonText, message);
                    default:
                        throw new Exception(message);
                }
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
        /// <param name="accountId"></param>
        [Route("{orderId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [ServiceFilter(typeof(MarginTradingEnabledFilter))]
        [HttpDelete]
        public Task CancelAsync(string orderId, [FromBody] OrderCancelRequest request = null, string accountId = null)
        {
            if (!_ordersCache.TryGetOrderById(orderId, out var order))
                throw new InvalidOperationException("Order not found");

            ValidationHelper.ValidateAccountId(order, accountId);

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
        /// Cancel order bulk
        /// </summary>
        [Route("bulk")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [ServiceFilter(typeof(MarginTradingEnabledFilter))]
        [HttpDelete]
        public async Task<Dictionary<string, string>> CancelBulkAsync([FromBody] OrderCancelBulkRequest request = null,
            string accountId = null)
        {
            var failedOrderIds = new Dictionary<string, string>();

            foreach (var id in request.OrderIds)
            {
                try
                {
                    await CancelAsync(id, request.OrderCancelRequest, accountId);
                }
                catch (Exception exception)
                {
                    await _log.WriteWarningAsync(nameof(OrdersController), nameof(CancelBulkAsync),
                        $"Failed to cancel order {id}", exception);
                    failedOrderIds.Add(id, exception.Message);
                }
            }

            return failedOrderIds;
        }

        /// <summary>
        /// Close group of orders by accountId, assetPairId and direction.
        /// </summary>
        /// <param name="accountId">Mandatory</param>
        /// <param name="assetPairId">Optional</param>
        /// <param name="direction">Optional</param>
        /// <param name="includeLinkedToPositions">Optional, should orders, linked to positions, to be canceled</param>
        /// <param name="request">Optional</param>
        /// <returns>Dictionary of failed to close orderIds with exception message</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        [Route("close-group")]//todo: to be deleted
        [Route("cancel-group")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [ServiceFilter(typeof(MarginTradingEnabledFilter))]
        [HttpDelete]
        public async Task<Dictionary<string, string>> CancelGroupAsync([FromQuery] string accountId,
            [FromQuery] string assetPairId = null,
            [FromQuery] OrderDirectionContract? direction = null,
            [FromQuery] bool includeLinkedToPositions = false,
            [FromBody] OrderCancelRequest request = null)
        {
            accountId.RequiredNotNullOrWhiteSpace(nameof(accountId));

            var failedOrderIds = new Dictionary<string, string>();

            foreach (var order in _ordersCache.GetPending()
                .Where(x => x.AccountId == accountId
                            && (string.IsNullOrEmpty(assetPairId) || x.AssetPairId == assetPairId)
                            && (direction == null || x.Direction == direction.ToType<OrderDirection>())
                            && (includeLinkedToPositions || string.IsNullOrEmpty(x.ParentPositionId))))
            {
                try
                {
                    await CancelAsync(order.Id, request, accountId);
                }
                catch (Exception exception)
                {
                    await _log.WriteWarningAsync(nameof(OrdersController), nameof(CancelGroupAsync),
                        $"Failed to cancel order {order.Id}", exception);
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
        public async Task ChangeAsync(string orderId, [FromBody] OrderChangeRequest request)
        {
            if (!_ordersCache.TryGetOrderById(orderId, out var order))
            {
                throw new InvalidOperationException("Order not found");
            }

            ValidationHelper.ValidateAccountId(order, request.AccountId);

            try
            {
                var originator = GetOriginator(request.Originator);

                var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
                    ? _identityGenerator.GenerateGuid()
                    : request.CorrelationId;

                await _tradingEngine.ChangeOrderAsync(order.Id, request.Price, request.Validity, originator,
                    request.AdditionalInfo, correlationId, request.ForceOpen);
            }
            catch (ValidateOrderException ex) when (ex is not ValidateOrderFunctionalException)
            {
                throw new InvalidOperationException(ex.Message);
            }

            _operationsLogService.AddLog("action order.changeLimits", order.AccountId,
                new { orderId = orderId, request = request.ToJson() }.ToJson(), "");
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