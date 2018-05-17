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
        private readonly IOrdersHistoryRepository _ordersHistoryRepository;
        private readonly IOrdersByIdRepository _ordersByIdRepository;

        public OrdersController(
            IAssetPairsCache assetPairsCache,
            ITradingEngine tradingEngine,
            IAccountsCacheService accountsCacheService,
            IMarginTradingOperationsLogService operationsLogService,
            IConsole consoleWriter,
            OrdersCache ordersCache,
            IAssetPairDayOffService assetDayOffService,
            IIdentityGenerator identityGenerator, 
            IOrdersHistoryRepository ordersHistoryRepository, 
            IOrdersByIdRepository ordersByIdRepository)
        {
            _assetPairsCache = assetPairsCache;
            _tradingEngine = tradingEngine;
            _accountsCacheService = accountsCacheService;
            _operationsLogService = operationsLogService;
            _consoleWriter = consoleWriter;
            _ordersCache = ordersCache;
            _assetDayOffService = assetDayOffService;
            _identityGenerator = identityGenerator;
            _ordersHistoryRepository = ordersHistoryRepository;
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

            _consoleWriter.WriteLine(
                $"action order.cancel for accountId = {order.AccountId}, orderId = {orderId}");
            _operationsLogService.AddLog("action order.cancel", order.AccountId, request.ToJson(),
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
            _operationsLogService.AddLog("action order.changeLimits", order.AccountId, request.ToJson(),
                "");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Get order by id 
        /// </summary>
        [HttpGet, Route("{orderId}")]
        public async Task<OrderContract> GetAsync(string orderId)
        {
            var (realOrderId, isCloseOrder) = ParseFakeOrderId(orderId);
            if (!isCloseOrder && _ordersCache.TryGetOrderById(realOrderId, out var order))
                return Convert(order);

            var orderById = await _ordersByIdRepository.GetAsync(realOrderId);
            if (orderById == null)
                return null;

            var history = await _ordersHistoryRepository.GetHistoryAsync(new[] {orderById.AccountId},
                orderById.OrderCreatedTime - TimeSpan.FromSeconds(1), null);

            if (!history.Any())
                return null;

            var lastHistoryRecord =
                history.Where(h => h.Id == orderId).OrderByDescending(h => h.UpdateTimestamp).First();

            if (isCloseOrder && lastHistoryRecord.Status != OrderStatus.Closed)
                return null;

            return Convert(lastHistoryRecord, isCloseOrder);
        }

        private (string RealOrderId, bool IsCloseOrder) ParseFakeOrderId(string orderId)
        {
            return orderId.EndsWith(CloseOrderIdSiffix)
                ? (orderId.Substring(0, orderId.Length - CloseOrderIdSiffix.Length), true)
                : (orderId, false);
        }

        /// <summary>
        /// Get orders by parent order id
        /// </summary>
        [HttpGet, Route("by-parent-order/{parentOrderId}")]
        public async Task<List<OrderContract>> ListByParentOrderAsync(string parentOrderId)
        {
            return new List<OrderContract>(); // todo
        }

        /// <summary>
        /// Get orders by parent position id
        /// </summary>
        [HttpGet, Route("by-parent-position/{parentPositionId}")]
        public async Task<List<OrderContract>> ListByParentPositionAsync(string parentPositionId)
        {
            var orderById = await _ordersByIdRepository.GetAsync(parentPositionId);
            if (orderById == null)
                return new List<OrderContract>();

            var history = await _ordersHistoryRepository.GetHistoryAsync(new[] {orderById.AccountId},
                orderById.OrderCreatedTime - TimeSpan.FromSeconds(1), null);

            var lastHistoryRecords = history.Where(h => h.ParentPositionId == parentPositionId)
                .OrderByDescending(h => h.UpdateTimestamp).GroupBy(o => o.Id,
                    (id, orders) => orders.OrderByDescending(h => h.UpdateTimestamp).First());

            return lastHistoryRecords.SelectMany(MakeOrderContractsFromHistory).ToList();
        }

        /// <summary>
        /// Get open orders with optional filtering
        /// </summary>
        [HttpGet, Route("open")]
        public async Task<List<OrderContract>> ListOpenAsync(string accountId = null, string assetPairId = null)
        {
            // do not call get by account, it's slower for single account 
            IEnumerable<Order> orders = _ordersCache.WaitingForExecutionOrders.GetAllOrders(); 
            
            if (!string.IsNullOrWhiteSpace(accountId))
                orders = orders.Where(o => o.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(assetPairId))
                orders = orders.Where(o => o.Instrument == assetPairId);

            return orders.Select(Convert).ToList(); // do not show a pending close order for closing status
        }

        //todo: move to history
        /// <summary>
        /// Get executed orders with optional filtering
        /// </summary>
        [HttpGet, Route("executed")]
        public async Task<List<OrderContract>> ListExecutedAsync(string accountId, string assetPairId)
        {
            var history = !string.IsNullOrWhiteSpace(accountId)
                ? await _ordersHistoryRepository.GetHistoryAsync(new[] {accountId}, null, null)
                : await _ordersHistoryRepository.GetHistoryAsync();

            if (!string.IsNullOrWhiteSpace(assetPairId))
                history = history.Where(o => o.Instrument == assetPairId);

            var pendingIds = _ordersCache.WaitingForExecutionOrders.GetAllOrders().Select(o => o.Id).ToHashSet();

            return history.Where(h => !pendingIds.Contains(h.Id)).SelectMany(MakeOrderContractsFromHistory).ToList();
        }

        private static OrderContract Convert(IOrderHistory history, bool isCloseOrder)
        {
            var orderDirection = GetOrderDirection(history.Type, isCloseOrder);
            return new OrderContract
            {
                Id = history.Id + (isCloseOrder ? CloseOrderIdSiffix : ""),
                AccountId = history.AccountId,
                AssetPairId = history.Instrument,
                CreatedTimestamp = history.CreateDate,
                Direction = history.Type.ToType<OrderDirectionContract>(),
                ExecutionPrice = history.OpenPrice,
                ExpectedOpenPrice = history.ExpectedOpenPrice,
                ForceOpen = true,
                ModifiedTimestamp = history.UpdateTimestamp,
                Originator = OriginatorTypeContract.Investor,
                ParentOrderId = history.ParentOrderId,
                PositionId = history.Id,
                RelatedOrders = new List<string>(),
                Status = Convert(history.Status),
                TradesIds = GetTrades(history.Id, history.Status, orderDirection),
                Type = history.OpenPrice == null ? OrderTypeContract.Market : OrderTypeContract.Limit,
                ValidityTime = null,
                Volume = history.Volume,
            };
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
                    return OrderStatusContract.Executed; // todo: fix
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
                PositionId = order.Id,
                RelatedOrders = new List<string>(),
                Status = Convert(order.Status),
                TradesIds = GetTrades(order.Id, order.Status, orderDirection),
                Type = order.OpenPrice == null ? OrderTypeContract.Market : OrderTypeContract.Limit,
                ValidityTime = null,
                Volume = order.Volume,
            };
        }

        private static IEnumerable<OrderContract> MakeOrderContractsFromHistory(IOrderHistory r)
        {
            yield return Convert(r, false);

            if (r.Status == OrderStatus.Closed)
                yield return Convert(r, true);
        }
    }
}
