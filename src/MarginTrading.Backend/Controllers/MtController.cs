using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Attributes;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.Trading;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Middleware;
using MarginTrading.Common.Services;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/mt")]
    public class MtController : Controller, ITradingApi
    {
        private readonly IMarginTradingAccountHistoryRepository _accountsHistoryRepository;
        private readonly IMarginTradingOrdersHistoryRepository _ordersHistoryRepository;
        private readonly IMicrographCacheService _micrographCacheService;
        private readonly IAccountAssetsCacheService _accountAssetsCacheService;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IMarketMakerMatchingEngine _matchingEngine;
        private readonly ITradingEngine _tradingEngine;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IMarginTradingOperationsLogService _operationsLogService;
        private readonly IConsole _consoleWriter;
        private readonly OrdersCache _ordersCache;
        private readonly MarginTradingSettings _marginSettings;
        private readonly AccountManager _accountManager;
        private readonly IAssetPairDayOffService _assetDayOffService;
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly IIdentityGenerator _identityGenerator;

        public MtController(
            IMarginTradingAccountHistoryRepository accountsHistoryRepository,
            IMarginTradingOrdersHistoryRepository ordersHistoryRepository,
            IMicrographCacheService micrographCacheService,
            IAccountAssetsCacheService accountAssetsCacheService,
            IAssetPairsCache assetPairsCache,
            IMarketMakerMatchingEngine matchingEngine,
            ITradingEngine tradingEngine,
            IAccountsCacheService accountsCacheService,
            IMarginTradingOperationsLogService operationsLogService,
            IConsole consoleWriter,
            OrdersCache ordersCache,
            MarginTradingSettings marginSettings,
            AccountManager accountManager,
            IAssetPairDayOffService assetDayOffService,
            IQuoteCacheService quoteCacheService,
            IIdentityGenerator identityGenerator)
        {
            _accountsHistoryRepository = accountsHistoryRepository;
            _ordersHistoryRepository = ordersHistoryRepository;
            _micrographCacheService = micrographCacheService;
            _accountAssetsCacheService = accountAssetsCacheService;
            _assetPairsCache = assetPairsCache;
            _matchingEngine = matchingEngine;
            _tradingEngine = tradingEngine;
            _accountsCacheService = accountsCacheService;
            _operationsLogService = operationsLogService;
            _consoleWriter = consoleWriter;
            _ordersCache = ordersCache;
            _marginSettings = marginSettings;
            _accountManager = accountManager;
            _assetDayOffService = assetDayOffService;
            _quoteCacheService = quoteCacheService;
            _identityGenerator = identityGenerator;
        }
        
        
        #region Init data

        [Route("init.prices")]
        [HttpPost]
        [SkipMarginTradingEnabledCheck]
        public Dictionary<string, InstrumentBidAskPairContract> InitPrices([FromBody]InitPricesBackendRequest request)
        {
            IEnumerable<KeyValuePair<string, InstrumentBidAskPair>> allQuotes = _quoteCacheService.GetAllQuotes();

            if (request.AssetIds != null && request.AssetIds.Any())
            {
                allQuotes = allQuotes.Where(q => request.AssetIds.Contains(q.Key));
            }

            return allQuotes.ToDictionary(q => q.Key, q => q.Value.ToBackendContract());
        }

        #endregion

        
        #region Order

        [Route("order.place")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpPost]
        public async Task<OpenOrderBackendResponse> PlaceOrder([FromBody]OpenOrderBackendRequest request)
        {
            var code = await _identityGenerator.GenerateIdAsync(nameof(Order));
            
            var order = new Order
            {
                Id = Guid.NewGuid().ToString("N"),
                Code = code,
                CreateDate = DateTime.UtcNow,
                ClientId = request.ClientId,
                AccountId = request.Order.AccountId,
                Instrument = request.Order.Instrument,
                Volume = request.Order.Volume,
                ExpectedOpenPrice = request.Order.ExpectedOpenPrice,
                TakeProfit = request.Order.TakeProfit,
                StopLoss = request.Order.StopLoss
            };

            var placedOrder = await _tradingEngine.PlaceOrderAsync(order);

            var result = BackendContractFactory.CreateOpenOrderBackendResponse(placedOrder);

            _consoleWriter.WriteLine($"action order.place for clientId = {request.ClientId}");
            _operationsLogService.AddLog("action order.place", request.ClientId, request.Order.AccountId, request.ToJson(), result.ToJson());

            return result;
        }

        [Route("order.close")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpPost]
        public async Task<BackendResponse<bool>> CloseOrder([FromBody] CloseOrderBackendRequest request)
        {
            if (!_ordersCache.ActiveOrders.TryGetOrderById(request.OrderId, out var order))
            {
                return BackendResponse<bool>.Error("Order not found");
            }

            if (_assetDayOffService.IsDayOff(order.Instrument))
            {
                return BackendResponse<bool>.Error("Trades for instrument are not available");
            }
            
            if (order.ClientId != request.ClientId || order.AccountId != request.AccountId)
            {
                return BackendResponse<bool>.Error("Order is not available for user");
            }

            if (request.IsForcedByBroker && string.IsNullOrEmpty(request.Comment))
            {
                return BackendResponse<bool>.Error("For operation forced by broker, comment is mandatory");
            }

            var reason = request.IsForcedByBroker ? OrderCloseReason.ClosedByBroker : OrderCloseReason.Close;

            order = await _tradingEngine.CloseActiveOrderAsync(request.OrderId, reason, request.Comment);

            var result = new BackendResponse<bool>
            {
                Result = order.Status == OrderStatus.Closed || order.Status == OrderStatus.Closing,
                Message = order.CloseRejectReasonText
            };

            _consoleWriter.WriteLine(
                $"action order.close for clientId = {request.ClientId}, orderId = {request.OrderId}");
            _operationsLogService.AddLog("action order.close", request.ClientId, order.AccountId, request.ToJson(),
                result.ToJson());

            return result;
        }

        [Route("order.cancel")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpPost]
        public async Task<BackendResponse<bool>> CancelOrder([FromBody] CloseOrderBackendRequest request)
        {
            if (!_ordersCache.WaitingForExecutionOrders.TryGetOrderById(request.OrderId, out var order))
            {
                return BackendResponse<bool>.Error("Order not found");
            }

            if (_assetDayOffService.IsDayOff(order.Instrument))
            {
                return BackendResponse<bool>.Error("Trades for instrument are not available");
            }

            if (order.ClientId != request.ClientId || order.AccountId != request.AccountId)
            {
                
                return BackendResponse<bool>.Error("Order is not available for user");
            }
            
            if (request.IsForcedByBroker && string.IsNullOrEmpty(request.Comment))
            {
                return BackendResponse<bool>.Error("For operation forced by broker, comment is mandatory");
            }

            var reason = request.IsForcedByBroker ? OrderCloseReason.CanceledByBroker : OrderCloseReason.Canceled;

            _tradingEngine.CancelPendingOrder(order.Id, reason, request.Comment);

            var result = new BackendResponse<bool> {Result = true};

            _consoleWriter.WriteLine(
                $"action order.cancel for clientId = {request.ClientId}, orderId = {request.OrderId}");
            _operationsLogService.AddLog("action order.cancel", request.ClientId, order.AccountId, request.ToJson(),
                result.ToJson());

            return result;
        }

        [Route("order.changeLimits")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpPost]
        public MtBackendResponse<bool> ChangeOrderLimits([FromBody]ChangeOrderLimitsBackendRequest request)
        {
            if (!_ordersCache.TryGetOrderById(request.OrderId, out var order))
            {
                return new MtBackendResponse<bool> {Message = "Order not found"};
            }
            
            if (_assetDayOffService.IsDayOff(order.Instrument))
            {
                return new MtBackendResponse<bool> { Message = "Trades for instrument are not available" };
            }

            try
            {
                _tradingEngine.ChangeOrderLimits(request.OrderId, request.StopLoss, request.TakeProfit,
                    request.ExpectedOpenPrice);
            }
            catch (ValidateOrderException ex)
            {
                return new MtBackendResponse<bool> {Result = false, Message = ex.Message};
            }

            var result = new MtBackendResponse<bool> {Result = true};

            _consoleWriter.WriteLine($"action order.changeLimits for clientId = {request.ClientId}, orderId = {request.OrderId}");
            _operationsLogService.AddLog("action order.changeLimits", request.ClientId, order.AccountId, request.ToJson(), result.ToJson());

            return result;
        }

        #endregion
        
        
        #region State
        
//        [Route("order.list")]
//        [HttpPost]
//        public OrderBackendContract[] GetOpenPositions([FromBody]ClientIdBackendRequest request)
//        {
//            var accountIds = _accountsCacheService.GetAll(request.ClientId).Select(item => item.Id).ToArray();
//
//            var positions = _ordersCache.ActiveOrders.GetOrdersByAccountIds(accountIds).Select(item => item.ToBackendContract()).ToList();
//            var orders = _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(accountIds).Select(item => item.ToBackendContract()).ToList();
//
//            positions.AddRange(orders);
//            var result = positions.ToArray();
//
//            return result;
//        }
//
//        [Route("order.account.list")]
//        [HttpPost]
//        public OrderBackendContract[] GetAccountOpenPositions([FromBody]AccountClientIdBackendRequest request)
//        {
//            var account = _accountsCacheService.Get(request.ClientId, request.AccountId);
//
//            var positions = _ordersCache.ActiveOrders.GetOrdersByAccountIds(account.Id).Select(item => item.ToBackendContract()).ToList();
//            var orders = _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(account.Id).Select(item => item.ToBackendContract()).ToList();
//
//            positions.AddRange(orders);
//            var result = positions.ToArray();
//
//            return result;
//        }
//
//        [Route("order.positions")]
//        [HttpPost]
//        public ClientOrdersBackendResponse GetClientOrders([FromBody]ClientIdBackendRequest request)
//        {
//            var accountIds = _accountsCacheService.GetAll(request.ClientId).Select(item => item.Id).ToArray();
//
//            var positions = _ordersCache.ActiveOrders.GetOrdersByAccountIds(accountIds).ToList();
//            var orders = _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(accountIds).ToList();
//
//            var result = BackendContractFactory.CreateClientOrdersBackendResponse(positions, orders);
//
//            return result;
//        }
        
        #endregion

    }
}
