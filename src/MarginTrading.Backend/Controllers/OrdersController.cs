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
using MarginTrading.Backend.Core.Orders;
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

        private const string CloseOrderIdSuffix = "_close";
        private readonly IOrdersByIdRepository _ordersByIdRepository;
        private readonly IDateService _dateService;

        public OrdersController(IAssetPairsCache assetPairsCache, ITradingEngine tradingEngine,
            IAccountsCacheService accountsCacheService, IMarginTradingOperationsLogService operationsLogService,
            IConsole consoleWriter, OrdersCache ordersCache, IAssetPairDayOffService assetDayOffService, 
            IOrdersByIdRepository ordersByIdRepository, IDateService dateService)
        {
            _assetPairsCache = assetPairsCache;
            _tradingEngine = tradingEngine;
            _accountsCacheService = accountsCacheService;
            _operationsLogService = operationsLogService;
            _consoleWriter = consoleWriter;
            _ordersCache = ordersCache;
            _assetDayOffService = assetDayOffService;
            _ordersByIdRepository = ordersByIdRepository;
            _dateService = dateService;
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
            

            var now = DateTime.UtcNow;
            
            var order = new Position
            {
                Id = Guid.NewGuid().ToString("N"),
                CreateDate = now,
                LastModified = now, 
                AccountId = request.AccountId,
                Instrument = request.InstrumentId,
                Volume = request.Direction == OrderDirectionContract.Buy ? request.Volume : -request.Volume,
                ExpectedOpenPrice = request.Price,
                TakeProfit = request.TakeProfit,
                StopLoss = request.StopLoss,
                OrderType = request.Type.ToType<OrderType>(),
                ForceOpen = request.ForceOpen,
                ParentOrderId = request.ParentOrderId,
                ParentPositionId = request.PositionId,
                Validity = request.Validity,
                Originator = request.Originator.ToType<OriginatorType>()
            };

            var placedOrder = await _tradingEngine.PlaceOrderAsync(order);

            _consoleWriter.WriteLine($"action order.place for accountId = {request.AccountId}");
            _operationsLogService.AddLog("action order.place", request.AccountId, request.ToJson(),
                placedOrder.ToJson());

            if (order.Status == PositionStatus.Rejected)
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
        public Task CancelAsync(string orderId/*, [FromBody] OrderCancelRequest request*/)
        {
            var isSl = orderId.Contains(OrderTypeContract.StopLoss.ToString());
            var isTp = orderId.Contains(OrderTypeContract.TakeProfit.ToString());
            
            if (isSl || isTp)
            {
                orderId = orderId
                    .Replace($"_{OrderTypeContract.StopLoss.ToString()}", "")
                    .Replace($"_{OrderTypeContract.TakeProfit.ToString()}", "");
                    
                if (!_ordersCache.TryGetOrderById(orderId, out var baseOrder))
                    throw new InvalidOperationException("Order not found");

                try
                {
                    _tradingEngine.ChangeOrderLimits(orderId, 
                        isSl ? null : baseOrder.StopLoss,
                        isTp ? null : baseOrder.TakeProfit,
                        baseOrder.ExpectedOpenPrice);
                }
                catch (ValidateOrderException ex)
                {
                    throw new InvalidOperationException(ex.Message);
                }

                _consoleWriter.WriteLine($"action order.changeLimits for orderId = {orderId}");
                _operationsLogService.AddLog("action order.changeLimits", baseOrder.AccountId, orderId, "");

                return Task.CompletedTask;
            }
            
            if (!_ordersCache.WaitingForExecutionOrders.TryGetOrderById(orderId, out var order))
                throw new InvalidOperationException("Order not found");

            var reason = OrderCloseReason.Canceled;
            
//            var reason =
//                request.Originator == OriginatorTypeContract.OnBehalf ||
//                request.Originator == OriginatorTypeContract.System
//                    ? OrderCloseReason.CanceledByBroker
//                    : OrderCloseReason.Canceled;

            var canceledOrder = _tradingEngine.CancelPendingOrder(order.Id, reason, "" /*request.Comment*/);

            _consoleWriter.WriteLine($"action order.cancel for accountId = {order.AccountId}, orderId = {orderId}");
            _operationsLogService.AddLog("action order.cancel", order.AccountId, "" /* request.ToJson()*/,
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
            var isSl = orderId.Contains(OrderTypeContract.StopLoss.ToString());
            var isTp = orderId.Contains(OrderTypeContract.TakeProfit.ToString());
            
            if (isSl || isTp)
            {
                orderId = orderId
                    .Replace($"_{OrderTypeContract.StopLoss.ToString()}", "")
                    .Replace($"_{OrderTypeContract.TakeProfit.ToString()}", "");
                
                if (!_ordersCache.TryGetOrderById(orderId, out var baseOrder))
                    throw new InvalidOperationException("Order not found");

                try
                {
                    _tradingEngine.ChangeOrderLimits(orderId, 
                        isSl ? request.Price : baseOrder.StopLoss,
                        isTp ? request.Price : baseOrder.TakeProfit,
                        baseOrder.ExpectedOpenPrice);
                }
                catch (ValidateOrderException ex)
                {
                    throw new InvalidOperationException(ex.Message);
                }

                _consoleWriter.WriteLine($"action order.changeLimits for orderId = {orderId}");
                _operationsLogService.AddLog("action order.changeLimits", baseOrder.AccountId, orderId, "");

                return Task.CompletedTask;
            }
            
            if (!_ordersCache.TryGetOrderById(orderId, out var order))
                throw new InvalidOperationException("Order not found");

            try
            {
                _tradingEngine.ChangeOrderLimits(order.Id, order.StopLoss, order.TakeProfit, request.Price);
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
            if (orderId.Contains(OrderTypeContract.StopLoss.ToString()))
            {
                if (!_ordersCache.TryGetOrderById(orderId.Replace($"_{OrderTypeContract.StopLoss.ToString()}", ""),
                    out var baseOrder))
                {
                    return Task.FromResult<OrderContract>(null);
                }

                return Task.FromResult(CreatePendingOrder(baseOrder, OrderTypeContract.StopLoss));
            }

            if (orderId.Contains(OrderTypeContract.TakeProfit.ToString()))
            {
                if (!_ordersCache.TryGetOrderById(orderId.Replace($"_{OrderTypeContract.TakeProfit.ToString()}", ""),
                    out var baseOrder))
                {
                    return Task.FromResult<OrderContract>(null);
                }
                
                return Task.FromResult(CreatePendingOrder(baseOrder, OrderTypeContract.TakeProfit));
            }

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
            IEnumerable<Position> orders = _ordersCache.GetAll();

            if (!string.IsNullOrWhiteSpace(accountId))
                orders = orders.Where(o => o.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(assetPairId))
                orders = orders.Where(o => o.Instrument == assetPairId);

            if (!string.IsNullOrWhiteSpace(parentPositionId))
                orders = orders.Where(o => o.Id == parentPositionId); // todo: fix when order will have a parentPositionId
            
            if (!string.IsNullOrWhiteSpace(parentOrderId))
                orders = orders.Where(o => o.Id == parentOrderId); // todo: fix when order will have a parentOrderId

            return Task.FromResult(orders.SelectMany(MakeOrderContracts).ToList());
        }

        private static List<string> GetTrades(string orderId, PositionStatus status, OrderDirection orderDirection)
        {
            if (status == PositionStatus.WaitingForExecution)
                return new List<string>();

            return new List<string> {orderId + '_' + orderDirection};
        }

        private static OrderDirection GetOrderDirection(OrderDirection openDirection, bool isCloseOrder)
        {
            return !isCloseOrder ? openDirection :
                openDirection == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
        }

        private static OrderStatusContract Convert(PositionStatus orderStatus)
        {
            switch (orderStatus)
            {
                case PositionStatus.WaitingForExecution:
                    return OrderStatusContract.Active;
                case PositionStatus.Active:
                    return OrderStatusContract.Executed; // todo: fix when orders
                case PositionStatus.Closed:
                    return OrderStatusContract.Executed;
                case PositionStatus.Rejected:
                    return OrderStatusContract.Rejected;
                case PositionStatus.Closing:
                    return OrderStatusContract.Executed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderStatus), orderStatus, null);
            }
        }
        
        private static IEnumerable<OrderContract> MakeOrderContracts(Position r)
        {
            var baseOrder = Convert(r);

            if (r.StopLoss != null)
            {
                var slOrder = CreatePendingOrder(r, OrderTypeContract.StopLoss);
                baseOrder.RelatedOrders.Add(slOrder.Id);
                yield return slOrder;
            }
            
            if (r.TakeProfit != null)
            {
                var tpOrder = CreatePendingOrder(r, OrderTypeContract.TakeProfit);
                baseOrder.RelatedOrders.Add(tpOrder.Id);
                yield return tpOrder;
            }

            if (baseOrder.Status != OrderStatusContract.Executed)
                yield return baseOrder;
        }

        private static OrderContract Convert(Position order)
        {
            var orderDirection = GetOrderDirection(order.GetOrderDirection(), false);
            return new OrderContract
            {
                Id = order.Id,
                AccountId = order.AccountId,
                AssetPairId = order.Instrument,
                CreatedTimestamp = order.CreateDate,
                Direction = order.GetOrderDirection().ToType<OrderDirectionContract>(),
                ExecutionPrice = order.OpenPrice,
                FxRate = order.GetFplRate(),
                ExpectedOpenPrice = order.ExpectedOpenPrice,
                ForceOpen = true,
                ModifiedTimestamp = order.LastModified ?? order.OpenDate ?? order.CreateDate,
                Originator = OriginatorTypeContract.Investor,
                ParentOrderId = null,
                PositionId = order.Status == PositionStatus.Active ? order.Id : null,
                RelatedOrders = new List<string>(),
                Status = Convert(order.Status),
                TradesIds = GetTrades(order.Id, order.Status, orderDirection),
                Type = order.ExpectedOpenPrice == null ? OrderTypeContract.Market : OrderTypeContract.Limit,
                ValidityTime = null,
                Volume = order.Volume,
            };
        }
        
        private static OrderContract CreatePendingOrder(Position order, OrderTypeContract type)
        {
            var result = Convert(order);

            result.Type = type;
            result.Status = result.Status == OrderStatusContract.Executed
                ? OrderStatusContract.Active
                : OrderStatusContract.Inactive;
            result.ParentOrderId = result.Id;
            result.Id += $"_{type}";
            result.ExecutionPrice = null;
            result.ExpectedOpenPrice = type == OrderTypeContract.StopLoss ? order.StopLoss : order.TakeProfit;
            result.Direction = result.Direction == OrderDirectionContract.Buy
                ? OrderDirectionContract.Sell
                : OrderDirectionContract.Buy;
            result.ExecutionPrice = null;
            result.ForceOpen = false;
            result.RelatedOrders = new List<string>();
            result.TradesIds = new List<string>();
            result.Volume = -result.Volume;

            return result;
        }
    }
}