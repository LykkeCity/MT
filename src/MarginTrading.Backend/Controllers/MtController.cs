using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Attributes;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Common.Wamp;
using MarginTrading.Core;
using MarginTrading.Core.Assets;
using MarginTrading.Core.Exceptions;
using MarginTrading.Core.MatchingEngines;
using MarginTrading.Core.Settings;
using MarginTrading.Services;
using MarginTrading.Services.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/mt")]
    public class MtController : Controller
    {
        private readonly IMarginTradingAccountHistoryRepository _accountsHistoryRepository;
        private readonly IMarginTradingOrdersHistoryRepository _ordersHistoryRepository;
        private readonly IMicrographCacheService _micrographCacheService;
        private readonly IAccountAssetsCacheService _accountAssetsCacheService;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IInternalMatchingEngine _matchingEngine;
        private readonly ITradingEngine _tradingEngine;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IMarginTradingOperationsLogService _operationsLogService;
        private readonly IConsole _consoleWriter;
        private readonly OrdersCache _ordersCache;
        private readonly MarginSettings _marginSettings;
        private readonly AccountManager _accountManager;
        private readonly IAssetPairDayOffService _assetDayOffService;
        private readonly IQuoteCacheService _quoteCacheService;

        public MtController(
            IMarginTradingAccountHistoryRepository accountsHistoryRepository,
            IMarginTradingOrdersHistoryRepository ordersHistoryRepository,
            IMicrographCacheService micrographCacheService,
            IAccountAssetsCacheService accountAssetsCacheService,
            IAssetPairsCache assetPairsCache,
            IInternalMatchingEngine matchingEngine,
            ITradingEngine tradingEngine,
            IAccountsCacheService accountsCacheService,
            IMarginTradingOperationsLogService operationsLogService,
            IConsole consoleWriter,
            OrdersCache ordersCache,
            MarginSettings marginSettings,
            AccountManager accountManager,
            IAssetPairDayOffService assetDayOffService,
            IQuoteCacheService quoteCacheService)
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
        }

        #region Init data

        [Route("init.data")]
        [HttpPost]
        public async Task<InitDataBackendResponse> InitData([FromBody]ClientIdBackendRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.ClientId))
            {
                throw new ArgumentNullException(nameof(ClientIdBackendRequest.ClientId));
            }

            var accounts = _accountsCacheService.GetAll(request.ClientId).ToArray();

            if (accounts.Length == 0 && !_marginSettings.IsLive)
            {
                accounts = await _accountManager.CreateDefaultAccounts(request.ClientId);
            }

            if (accounts.Length == 0)
                return InitDataBackendResponse.CreateEmpty();

            var assets = _accountAssetsCacheService.GetClientAssets(accounts);

            var result = InitDataBackendResponse.Create(accounts, assets, _marginSettings.IsLive);
            result.IsLive = _marginSettings.IsLive;

            return result;
        }

        /// <summary>
        /// uses in BoxOptions app only
        /// </summary>
        /// <returns></returns>
        [Route("init.chartdata")]
        [HttpPost]
        public InitChartDataBackendResponse InitChardData()
        {
            var chartData = _micrographCacheService.GetGraphData();
            return InitChartDataBackendResponse.Create(chartData);
        }

        [Route("init.accounts")]
        [HttpPost]
        public MarginTradingAccountBackendContract[] InitAccounts([FromBody]ClientIdBackendRequest request)
        {
            var accounts = _accountsCacheService.GetAll(request.ClientId).ToArray();

            var result = accounts.Select(item => item.ToBackendContract(_marginSettings.IsLive)).ToArray();

            return result;
        }

        [Route("init.accountinstruments")]
        [HttpPost]
        public InitAccountInstrumentsBackendResponse AccountInstruments([FromBody]ClientIdBackendRequest request)
        {
            var accounts = _accountsCacheService.GetAll(request.ClientId).ToArray();

            if (accounts.Length == 0)
                return InitAccountInstrumentsBackendResponse.CreateEmpty();

            var accountAssets = _accountAssetsCacheService.GetClientAssets(accounts);
            var result = InitAccountInstrumentsBackendResponse.Create(accountAssets);

            return result;
        }

        [Route("init.graph")]
        [HttpPost]
        [SkipMarginTradingEnabledCheck]
        public InitChartDataBackendResponse InitGraph([FromBody]InitChartDataBackendRequest request)
        {
            var chartData = _micrographCacheService.GetGraphData();

            if (request?.AssetIds?.Length > 0)
            {
                chartData = chartData.Where(d => request.AssetIds.Contains(d.Key))
                    .ToDictionary(k => k.Key, v => v.Value);
            }

            return InitChartDataBackendResponse.Create(chartData);
        }

        [Route("init.availableassets")]
        [HttpPost]
        public string[] InitAvailableAssets([FromBody]ClientIdBackendRequest request)
        {
            var result = new List<string>();
            var accounts = _accountsCacheService.GetAll(request.ClientId);

            foreach (var account in accounts)
            {
                result.AddRange(_accountAssetsCacheService.GetAccountAssets(account.TradingConditionId, account.BaseAssetId).Select(a => a.Instrument));
            }

            return result.Distinct().ToArray();
        }

        [Route("init.assets")]
        [HttpPost]
        public AssetPairBackendContract[] InitAssets()
        {
            var instruments = _assetPairsCache.GetAll();
            return instruments
                .Select(item => item.ToBackendContract()).ToArray();
        }

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

        #region Account

        [Route("account.history")]
        [HttpPost]
        public async Task<AccountHistoryBackendResponse> GetAccountHistory([FromBody]AccountHistoryBackendRequest request)
        {
            var clientAccountIds = string.IsNullOrEmpty(request.AccountId)
                    ? _accountsCacheService.GetAll(request.ClientId).Select(item => item.Id).ToArray()
                    : new[] { request.AccountId };

            var accounts = (await _accountsHistoryRepository.GetAsync(clientAccountIds, request.From, request.To))
                .Where(item => item.Type != AccountHistoryType.OrderClosed);

            var orders = (await _ordersHistoryRepository.GetHistoryAsync(request.ClientId, clientAccountIds, request.From, request.To))
                .Where(item => item.Status != OrderStatus.Rejected);

            var openPositions = _ordersCache.ActiveOrders.GetOrdersByAccountIds(clientAccountIds).ToList();

            var result = AccountHistoryBackendResponse.Create(accounts, openPositions, orders);

            _consoleWriter.WriteLine($"action account.history for clientId = {request.ClientId}");
            _operationsLogService.AddLog("action account.history", request.ClientId, request.AccountId,
                request.ToJson(), $"Account items: {result.Account.Length}, open positions count: {result.OpenPositions.Length}, history items: {result.PositionsHistory.Length}");

            return result;
        }

        [Route("account.history.new")]
        [HttpPost]
        public async Task<AccountNewHistoryBackendResponse> GetHistory([FromBody]AccountHistoryBackendRequest request)
        {
            var clientAccountIds = string.IsNullOrEmpty(request.AccountId)
                    ? _accountsCacheService.GetAll(request.ClientId).Select(item => item.Id).ToArray()
                    : new[] { request.AccountId };

            var accounts = (await _accountsHistoryRepository.GetAsync(clientAccountIds, request.From, request.To))
                .Where(item => item.Type != AccountHistoryType.OrderClosed);

            var openOrders = _ordersCache.ActiveOrders.GetOrdersByAccountIds(clientAccountIds);

            var historyOrders = (await _ordersHistoryRepository.GetHistoryAsync(request.ClientId, clientAccountIds, request.From, request.To))
                .Where(item => item.Status != OrderStatus.Rejected);

            var result = AccountNewHistoryBackendResponse.Create(accounts, openOrders, historyOrders);

            _consoleWriter.WriteLine($"action account.history.new for clientId = {request.ClientId}");
            _operationsLogService.AddLog("action account.history.new", request.ClientId, request.AccountId,
                request.ToJson(), $"history items count: {result.HistoryItems.Length}");

            return result;
        }

        #endregion

        #region Order

        [Route("order.place")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpPost]
        public async Task<OpenOrderBackendResponse> PlaceOrder([FromBody]OpenOrderBackendRequest request)
        {
            var order = new Order
            {
                Id = Guid.NewGuid().ToString("N"),
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

            var result = OpenOrderBackendResponse.Create(placedOrder);

            _consoleWriter.WriteLine($"action order.place for clientId = {request.ClientId}");
            _operationsLogService.AddLog("action order.place", request.ClientId, request.Order.AccountId, request.ToJson(), result.ToJson());

            return result;
        }

        [Route("order.close")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpPost]
        public async Task<MtBackendResponse<bool>> CloseOrder([FromBody] CloseOrderBackendRequest request)
        {
            var order = _ordersCache.ActiveOrders.GetOrderById(request.OrderId);

            if (_assetDayOffService.IsDayOff(order.Instrument))
            {
                return new MtBackendResponse<bool> {Message = "Trades for instrument are not available"};
            }

            order = await _tradingEngine.CloseActiveOrderAsync(request.OrderId, OrderCloseReason.Close);

            var result = new MtBackendResponse<bool> {Result = order.Status == OrderStatus.Closed};

            _consoleWriter.WriteLine(
                $"action order.close for clientId = {request.ClientId}, orderId = {request.OrderId}");
            _operationsLogService.AddLog("action order.close", request.ClientId, order.AccountId, request.ToJson(),
                result.ToJson());

            return result;
        }

        [Route("order.cancel")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpPost]
        public MtBackendResponse<bool> CancelOrder([FromBody] CloseOrderBackendRequest request)
        {
            var order = _ordersCache.WaitingForExecutionOrders.GetOrderById(request.OrderId);

            if (_assetDayOffService.IsDayOff(order.Instrument))
            {
                return new MtBackendResponse<bool> {Message = "Trades for instrument are not available"};
            }

            _tradingEngine.CancelPendingOrder(order.Id, OrderCloseReason.Canceled);

            var result = new MtBackendResponse<bool> {Result = true};

            _consoleWriter.WriteLine(
                $"action order.cancel for clientId = {request.ClientId}, orderId = {request.OrderId}");
            _operationsLogService.AddLog("action order.cancel", request.ClientId, order.AccountId, request.ToJson(),
                result.ToJson());

            return result;
        }

        [Route("order.list")]
        [HttpPost]
        public OrderBackendContract[] GetOpenPositions([FromBody]ClientIdBackendRequest request)
        {
            string[] accountIds = _accountsCacheService.GetAll(request.ClientId).Select(item => item.Id).ToArray();

            var positions = _ordersCache.ActiveOrders.GetOrdersByAccountIds(accountIds).Select(item => item.ToBackendContract()).ToList();
            var orders = _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(accountIds).Select(item => item.ToBackendContract()).ToList();

            positions.AddRange(orders);
            var result = positions.ToArray();

            return result;
        }

        [Route("order.account.list")]
        [HttpPost]
        public OrderBackendContract[] GetAccountOpenPositions([FromBody]AccountClientIdBackendRequest request)
        {
            var account = _accountsCacheService.Get(request.ClientId, request.AccountId);

            var positions = _ordersCache.ActiveOrders.GetOrdersByAccountIds(account.Id).Select(item => item.ToBackendContract()).ToList();
            var orders = _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(account.Id).Select(item => item.ToBackendContract()).ToList();

            positions.AddRange(orders);
            var result = positions.ToArray();

            return result;
        }

        [Route("order.positions")]
        [HttpPost]
        public ClientOrdersBackendResponse GetClientOrders([FromBody]ClientIdBackendRequest request)
        {
            string[] accountIds = _accountsCacheService.GetAll(request.ClientId).Select(item => item.Id).ToArray();

            var positions = _ordersCache.ActiveOrders.GetOrdersByAccountIds(accountIds).ToList();
            var orders = _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(accountIds).ToList();

            var result = ClientOrdersBackendResponse.Create(positions, orders);

            return result;
        }

        [Route("order.changeLimits")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpPost]
        public MtBackendResponse<bool> ChangeOrderLimits([FromBody]ChangeOrderLimitsBackendRequest request)
        {
            var order = _ordersCache.GetOrderById(request.OrderId);

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

        #region Orderbook

        [Route("orderbooks")]
        [HttpPost]
        public OrderbooksBackendResponse GetOrderBooks([FromBody] OrderbooksBackendRequest request)
        {
            return OrderbooksBackendResponse.Create(_matchingEngine.GetOrderBook(request.Instrument));
        }

        #endregion

        [Route("ping")]
        [HttpPost]
        public MtBackendResponse<string> Ping()
        {
            return new MtBackendResponse<string> { Result = $"[{DateTime.UtcNow:u}] Ping!" };
        }

    }
}
