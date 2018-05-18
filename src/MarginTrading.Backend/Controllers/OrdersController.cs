using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.AzureRepositories.Snow.OrdersById;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Repositories;
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

        private const string CloseOrderIdSiffix = "_close";
        private readonly IOrdersByIdRepository _ordersByIdRepository;

        public OrdersController(IAssetPairsCache assetPairsCache, ITradingEngine tradingEngine,
            IAccountsCacheService accountsCacheService, IMarginTradingOperationsLogService operationsLogService,
            IConsole consoleWriter, OrdersCache ordersCache, IAssetPairDayOffService assetDayOffService,
            IIdentityGenerator identityGenerator, IOrdersByIdRepository ordersByIdRepository)
        {
            _assetPairsCache = assetPairsCache;
            _tradingEngine = tradingEngine;
            _accountsCacheService = accountsCacheService;
            _operationsLogService = operationsLogService;
            _consoleWriter = consoleWriter;
            _ordersCache = ordersCache;
            _assetDayOffService = assetDayOffService;
            _identityGenerator = identityGenerator;
            _ordersByIdRepository = ordersByIdRepository;
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
                AccountId = request.AccountId,
                Instrument = request.InstrumentId,
                Volume = request.Direction == OrderDirectionContract.Buy ? request.Volume : -request.Volume,
                ExpectedOpenPrice = request.Price
            };

            var placedOrder = await _tradingEngine.PlaceOrderAsync(order);

            _consoleWriter.WriteLine($"action order.place for accountId = {request.AccountId}");
            _operationsLogService.AddLog("action order.place", request.AccountId, request.ToJson(),
                placedOrder.ToJson());

            if (order.Status == OrderStatus.Rejected)
            {
                throw new Exception($"Order is rejected: {order.RejectReason} ({order.RejectReasonText})");
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
        public Task CancelAsync(string orderId, [FromBody] OrderCancelRequest request)
        {
            if (!_ordersCache.WaitingForExecutionOrders.TryGetOrderById(orderId, out var order))
                throw new InvalidOperationException("Order not found");

            if (_assetDayOffService.IsDayOff(order.Instrument))
                throw new InvalidOperationException("Trades for instrument are not available");

            var reason =
                request.Originator == OriginatorTypeContract.OnBehalf ||
                request.Originator == OriginatorTypeContract.System
                    ? OrderCloseReason.CanceledByBroker
                    : OrderCloseReason.Canceled;

            var canceledOrder = _tradingEngine.CancelPendingOrder(order.Id, reason, request.Comment);

            _consoleWriter.WriteLine($"action order.cancel for accountId = {order.AccountId}, orderId = {orderId}");
            _operationsLogService.AddLog("action order.cancel", order.AccountId, request.ToJson(),
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

            if (_assetDayOffService.IsDayOff(order.Instrument))
                throw new InvalidOperationException("Trades for instrument are not available");

            try
            {
                _tradingEngine.ChangeOrderLimits(orderId, null, null, request.Price);
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
                ? Task.FromResult(Convert(order))
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
            IEnumerable<Order> orders = _ordersCache.WaitingForExecutionOrders.GetAllOrders();

            if (!string.IsNullOrWhiteSpace(accountId))
                orders = orders.Where(o => o.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(assetPairId))
                orders = orders.Where(o => o.Instrument == assetPairId);

            if (!string.IsNullOrWhiteSpace(parentPositionId))
                orders = orders.Where(o => o.Id == parentPositionId); // todo: fix when order will have a parentPositionId
            if (!string.IsNullOrWhiteSpace(parentPositionId))
                orders = orders.Where(o => o.Id == parentOrderId); // todo: fix when order will have a parentOrderId

            return Task.FromResult(orders.Select(Convert).ToList());
        }

        private static List<string> GetTrades(string orderId, OrderStatus status, OrderDirection orderDirection)
        {
            if (status == OrderStatus.WaitingForExecution)
                return new List<string>();

            return new List<string> {orderId + '_' + orderDirection};
        }

        private static OrderDirection GetOrderDirection(OrderDirection openDirection, bool isCloseOrder)
        {
            return !isCloseOrder ? openDirection :
                openDirection == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
        }

        private static OrderStatusContract Convert(OrderStatus orderStatus)
        {
            switch (orderStatus)
            {
                case OrderStatus.WaitingForExecution:
                    return OrderStatusContract.Active;
                case OrderStatus.Active:
                    return OrderStatusContract.Executed; // todo: fix when orders
                case OrderStatus.Closed:
                    return OrderStatusContract.Executed;
                case OrderStatus.Rejected:
                    return OrderStatusContract.Rejected;
                case OrderStatus.Closing:
                    return OrderStatusContract.Active;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderStatus), orderStatus, null);
            }
        }

        private static OrderContract Convert(Order order)
        {
            var orderDirection = GetOrderDirection(order.GetOrderType(), false);
            return new OrderContract
            {
                Id = order.Id,
                AccountId = order.AccountId,
                AssetPairId = order.Instrument,
                CreatedTimestamp = order.CreateDate,
                Direction = order.GetOrderType().ToType<OrderDirectionContract>(),
                ExecutionPrice = order.OpenPrice,
                ExpectedOpenPrice = order.ExpectedOpenPrice,
                ForceOpen = true,
                ModifiedTimestamp = order.OpenDate ?? order.CreateDate,
                Originator = OriginatorTypeContract.Investor,
                ParentOrderId = null,
                PositionId = order.Status == OrderStatus.Active ? order.Id : null,
                RelatedOrders = new List<string>(),
                Status = Convert(order.Status),
                TradesIds = GetTrades(order.Id, order.Status, orderDirection),
                Type = order.ExpectedOpenPrice == null ? OrderTypeContract.Market : OrderTypeContract.Limit,
                ValidityTime = null,
                Volume = order.Volume,
            };
        }
    }
}