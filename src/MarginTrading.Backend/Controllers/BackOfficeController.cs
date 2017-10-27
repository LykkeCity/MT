using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Attributes;
using MarginTrading.Backend.Models;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;
using MarginTrading.Core.MatchingEngines;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Settings;
using MarginTrading.Services;
using MarginTrading.Services.Infrastructure;
using MarginTrading.Services.MatchingEngines;
using MarginTrading.Services.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/backoffice")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class BackOfficeController : Controller
    {
        private readonly ITradingConditionsCacheService _tradingConditionsCacheService;
        private readonly IAccountGroupCacheService _accountGroupCacheService;
        private readonly AccountAssetsCacheService _accountAssetsCacheService;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly AccountManager _accountManager;
        private readonly TradingConditionsManager _tradingConditionsManager;
        private readonly AccountGroupManager _accountGroupManager;
        private readonly AccountAssetsManager _accountAssetsManager;
        private readonly MatchingEngineRoutesManager _routesManager;
        private readonly IMarginTradingAccountsRepository _accountsRepository;
        private readonly IOrderReader _ordersReader;
        private readonly OrderBookList _orderBooks;
        private readonly IClientSettingsRepository _clientSettingsRepository;
        private readonly MarginSettings _marginSettings;
        private readonly IMarginTradingOperationsLogService _operationsLogService;
        private readonly IConsole _consoleWriter;
        private readonly IMaintenanceModeService _maintenanceModeService;
        private readonly ILog _log;
        private readonly IMarginTradingSettingsService _marginTradingSettingsService;

        public BackOfficeController(
            ITradingConditionsCacheService tradingConditionsCacheService,
            IAccountGroupCacheService accountGroupCacheService,
            AccountAssetsCacheService accountAssetsCacheService,
            IAssetPairsCache assetPairsCache,
            IAccountsCacheService accountsCacheService,
            AccountManager accountManager,
            TradingConditionsManager tradingConditionsManager,
            AccountGroupManager accountGroupManager,
            AccountAssetsManager accountAssetsManager,
            MatchingEngineRoutesManager routesManager,
            IMarginTradingAccountsRepository accountsRepository,
            IOrderReader ordersReader,
            OrderBookList orderBooks,
            IClientSettingsRepository clientSettingsRepository,
            MarginSettings marginSettings,
            IMarginTradingOperationsLogService operationsLogService,
            IConsole consoleWriter,
            IMaintenanceModeService maintenanceModeService,
            ILog log,
            IMarginTradingSettingsService marginTradingSettingsService)
        {
            _tradingConditionsCacheService = tradingConditionsCacheService;
            _accountGroupCacheService = accountGroupCacheService;
            _accountAssetsCacheService = accountAssetsCacheService;
            _assetPairsCache = assetPairsCache;
            _accountsCacheService = accountsCacheService;

            _accountManager = accountManager;
            _tradingConditionsManager = tradingConditionsManager;
            _accountGroupManager = accountGroupManager;
            _accountAssetsManager = accountAssetsManager;
            _routesManager = routesManager;
            _accountsRepository = accountsRepository;
            _ordersReader = ordersReader;
            _orderBooks = orderBooks;
            _clientSettingsRepository = clientSettingsRepository;
            _marginSettings = marginSettings;
            _operationsLogService = operationsLogService;
            _consoleWriter = consoleWriter;
            _maintenanceModeService = maintenanceModeService;
            _log = log;
            _marginTradingSettingsService = marginTradingSettingsService;
        }


        #region Monitoring

        /// <summary>
        /// Returns summary asset info
        /// </summary>
        /// <remarks>
        /// VolumeLong is a sum of long positions volume
        ///
        /// VolumeShort is a sum of short positions volume
        ///
        /// PnL is a sum of all positions PnL
        ///
        /// Header "api-key" is required
        /// </remarks>
        /// <response code="200">Returns summary info by assets</response>
        [HttpGet]
        [Route("assetsInfo")]
        [ProducesResponseType(typeof(List<SummaryAssetInfo>), 200)]
        public List<SummaryAssetInfo> GetAssetsInfo()
        {
            var result = new List<SummaryAssetInfo>();
            var orders = _ordersReader.GetAll().ToList();

            foreach (var order in orders)
            {
                var assetInfo = result.FirstOrDefault(item => item.AssetPairId == order.Instrument);

                if (assetInfo == null)
                {
                    result.Add(new SummaryAssetInfo
                    {
                        AssetPairId = order.Instrument,
                        PnL = order.GetFpl(),
                        VolumeLong = order.GetOrderType() == OrderDirection.Buy ? order.GetMatchedVolume() : 0,
                        VolumeShort = order.GetOrderType() == OrderDirection.Sell ? order.GetMatchedVolume() : 0
                    });
                }
                else
                {
                    assetInfo.PnL += order.GetFpl();

                    if (order.GetOrderType() == OrderDirection.Buy)
                    {
                        assetInfo.VolumeLong += order.GetMatchedVolume();
                    }
                    else
                    {
                        assetInfo.VolumeShort += order.GetMatchedVolume();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns list of opened positions
        /// </summary>
        /// <remarks>
        /// Returns list of opened positions with matched volume greater or equal provided "volume" parameter
        ///
        /// Header "api-key" is required
        /// </remarks>
        /// <response code="200">Returns opened positions</response>
        [HttpGet]
        [Route("positionsByVolume")]
        [ProducesResponseType(typeof(List<OrderContract>), 200)]
        public List<OrderContract> GetPositionsByVolume([FromQuery]decimal volume)
        {
            var result = new List<OrderContract>();
            var orders = _ordersReader.GetActive();

            foreach (var order in orders)
            {
                if (order.GetMatchedVolume() >= volume)
                {
                    result.Add(order.ToBaseContract());
                }
            }

            return result;
        }

        /// <summary>
        /// Returns list of pending orders
        /// </summary>
        /// <remarks>
        /// Returns list of pending orders with volume greater or equal provided "volume" parameter
        ///
        /// Header "api-key" is required
        /// </remarks>
        /// <response code="200">Returns pending orders</response>
        [HttpGet]
        [Route("pendingOrdersByVolume")]
        [ProducesResponseType(typeof(List<OrderContract>), 200)]
        public List<OrderContract> GetPendingOrdersByVolume([FromQuery]decimal volume)
        {
            var result = new List<OrderContract>();
            var orders = _ordersReader.GetPending();

            foreach (var order in orders)
            {
                if (Math.Abs(order.Volume) >= volume)
                {
                    result.Add(order.ToBaseContract());
                }
            }

            return result;
        }

        #endregion


        #region Trading conditions

        /// <summary>
        /// Sets trading condition for account
        /// </summary>
        /// <remarks>
        ///
        /// Header "api-key" is required
        /// </remarks>
        /// <response code="200">Returns true if trading condition is set</response>
        [HttpPost]
        [Route("setTradingCondition")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> SetTradingCondition([FromBody]SetTradingConditionModel model)
        {
            bool result;

            var tradingCondition = _tradingConditionsCacheService.GetTradingCondition(model.TradingConditionId);

            if (tradingCondition == null)
            {
                throw new Exception($"No trading condition {model.TradingConditionId} found in cache");
            }

            _accountsCacheService.SetTradingCondition(model.ClientId, model.AccountId, model.TradingConditionId);

            result = await _accountsRepository.UpdateTradingConditionIdAsync(model.AccountId, model.TradingConditionId);

            if (result)
            {
                await _tradingConditionsManager.UpdateTradingConditions(model.TradingConditionId, model.AccountId);
            }

            return Ok(result);
        }

        [HttpGet]
        [Route("tradingConditions/getall")]
        [ProducesResponseType(typeof(List<MarginTradingCondition>), 200)]
        public IActionResult GetAllTradingConditions()
        {
            var tradingConditions = _tradingConditionsCacheService.GetAllTradingConditions();
            return Ok(tradingConditions);
        }

        [HttpGet]
        [Route("tradingConditions/get/{id}")]
        [ProducesResponseType(typeof(MarginTradingCondition), 200)]
        public IActionResult GetTradingCondition(string id)
        {
            var tradingCondition = _tradingConditionsCacheService.GetTradingCondition(id);
            return Ok(tradingCondition);
        }

        [HttpPost]
        [Route("tradingConditions/add")]
        public async Task<IActionResult> AddOrReplaceTradingCondition([FromBody]MarginTradingCondition model)
        {
            if (_tradingConditionsCacheService.GetTradingCondition(model.Id) == null)
            {
                await _accountGroupManager.AddAccountGroupsForTradingCondition(model.Id);
            }

            await _tradingConditionsManager.AddOrReplaceTradingConditionAsync(model);

            return Ok();
        }

        #endregion


        #region Account groups

        [HttpGet]
        [Route("accountGroups/getall")]
        [ProducesResponseType(typeof(List<MarginTradingAccountGroup>), 200)]
        public IActionResult GetAllAccountGrpups()
        {
            var accountGrpups = _accountGroupCacheService.GetAllAccountGroups();
            return Ok(accountGrpups);
        }

        [HttpGet]
        [Route("accountGroups/get/{tradingConditionId}/{id}")]
        [ProducesResponseType(typeof(MarginTradingAccountGroup), 200)]
        public IActionResult GetAccountGroup(string tradingConditionId, string id)
        {
            var accountGroup = _accountGroupCacheService.GetAccountGroup(tradingConditionId, id);
            return Ok(accountGroup);
        }

        [HttpPost]
        [Route("accountGroups/add")]
        public async Task<IActionResult> AddOrReplaceAccountGroup([FromBody]MarginTradingAccountGroup model)
        {
            await _accountGroupManager.AddOrReplaceAccountGroupAsync(model);
            await _tradingConditionsManager.UpdateTradingConditions(model.TradingConditionId);

            return Ok();
        }

        #endregion


        #region Account assets

        [HttpGet]
        [Route("accountAssets/getall/{tradingConditionId}/{baseAssetId}")]
        [ProducesResponseType(typeof(List<AccountAssetPair>), 200)]
        public IActionResult GetAllAccountAssets(string tradingConditionId, string baseAssetId)
        {
            var accountAssets = _accountAssetsCacheService.GetAccountAssets(tradingConditionId, baseAssetId);
            return Ok(accountAssets);
        }

        [HttpGet]
        [Route("accountAssets/get/{tradingConditionId}/{baseAssetId}/{instrumet}")]
        [ProducesResponseType(typeof(AccountAssetPair), 200)]
        public IActionResult GetAccountAssets(string tradingConditionId, string baseAssetId, string instrumet)
        {
            var accountAsset = _accountAssetsCacheService.GetAccountAssetThrowIfNotFound(tradingConditionId, baseAssetId, instrumet);
            return Ok(accountAsset);
        }

        [HttpPost]
        [Route("accountAssets/assignInstruments")]
        [ProducesResponseType(typeof(AccountAssetPair), 200)]
        public async Task<IActionResult> AssignInstruments([FromBody]AssignInstrumentsModel model)
        {
            await _accountAssetsManager.AssignInstruments(model.TradingConditionId, model.BaseAssetId, model.Instruments);
            await _tradingConditionsManager.UpdateTradingConditions(model.TradingConditionId);

            return Ok();
        }

        [HttpPost]
        [Route("accountAssets/add")]
        public async Task<IActionResult> AddOrReplaceAccountAsset([FromBody]AccountAssetPair model)
        {
            await _accountAssetsManager.AddOrReplaceAccountAssetAsync(model);
            await _tradingConditionsManager.UpdateTradingConditions(model.TradingConditionId);

            return Ok();
        }

        #endregion


        #region Dictionaries

        [HttpGet]
        [Route("instruments/getall")]
        [ProducesResponseType(typeof(List<AssetPair>), 200)]
        public IActionResult GetAllInstruments()
        {
            var instruments = _assetPairsCache.GetAll();
            return Ok(instruments);
        }

        [HttpGet]
        [Route("matchingengines")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public IActionResult GetAllMatchingEngines()
        {
            var matchingEngines = MatchingEngineConstants.All;
            return Ok(matchingEngines);
        }

        [HttpGet]
        [Route("orderTypes/getall")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public IActionResult GetAllOrderTypes()
        {
            var orderTypes = Enum.GetNames(typeof(OrderDirection));
            return Ok(orderTypes);
        }

        #endregion


        #region Accounts

        [HttpGet]
        [Route("marginTradingAccounts/getall/{clientId}")]
        [ProducesResponseType(typeof(List<MarginTradingAccount>), 200)]
        public IActionResult GetAllMarginTradingAccounts(string clientId)
        {
            var accounts = _accountsCacheService.GetAll(clientId);
            return Ok(accounts);
        }

        [HttpPost]
        [Route("marginTradingAccounts/delete/{clientId}/{accountId}")]
        public async Task<IActionResult> DeleteMarginTradingAccount(string clientId, string accountId)
        {
            await _accountManager.DeleteAccountAsync(clientId, accountId);
            return Ok();
        }

        [HttpPost]
        [Route("marginTradingAccounts/init")]
        public async Task<InitAccountsResponse> InitMarginTradingAccounts([FromBody]InitAccountsRequest request)
        {
            var accounts = _accountsCacheService.GetAll(request.ClientId);

            if (accounts.Any())
            {
                return new InitAccountsResponse { Status = CreateAccountStatus.Available };
            }

            if (string.IsNullOrEmpty(request.TradingConditionsId))
            {
                return new InitAccountsResponse
                {
                    Status = CreateAccountStatus.Error,
                    Message = "Can't create accounts - no trading condition passed"
                };
            }

            await _accountManager.CreateDefaultAccounts(request.ClientId, request.TradingConditionsId);

            return new InitAccountsResponse { Status = CreateAccountStatus.Created};
        }

        [HttpPost]
        [Route("marginTradingAccounts/add")]
        public async Task<IActionResult> AddMarginTradingAccount([FromBody]MarginTradingAccount account)
        {
            await _accountManager.AddAccountAsync(account.ClientId, account.BaseAssetId, account.TradingConditionId);
            return Ok();
        }

        [Route("marginTradingAccounts/deposit")]
        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> AccountDeposit([FromBody]AccountDepositWithdrawRequest request)
        {
            if (!_marginSettings.IsLive)
                return Ok(false);

            var account = _accountsCacheService.Get(request.ClientId, request.AccountId);

            var changeTransferLimit = request.PaymentType == PaymentType.Transfer && !IsCrypto(account.BaseAssetId); ;

            try
            {
                await _accountManager.UpdateBalanceAsync(request.ClientId, request.AccountId, Math.Abs(request.Amount),
                    AccountHistoryType.Deposit, "Account deposit", changeTransferLimit);
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(BackOfficeController), "AccountDeposit", request?.ToJson(), e);
                return Ok(false);
            }

            _consoleWriter.WriteLine($"account deposit for clientId = {request.ClientId}");
            _operationsLogService.AddLog($"account deposit {request.PaymentType}", request.ClientId, request.AccountId, request.ToJson(), true.ToJson());

            return Ok(true);
        }

        [Route("marginTradingAccounts/withdraw")]
        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> AccountWithdraw([FromBody]AccountDepositWithdrawRequest request)
        {
            if (!_marginSettings.IsLive)
                return Ok(false);

            var account = _accountsCacheService.Get(request.ClientId, request.AccountId);
            var freeMargin = account.GetFreeMargin();

            if (freeMargin < Math.Abs(request.Amount))
                return Ok(false);

            var changeTransferLimit = request.PaymentType == PaymentType.Transfer && !IsCrypto(account.BaseAssetId);

            try
            {
                await _accountManager.UpdateBalanceAsync(request.ClientId, request.AccountId, -Math.Abs(request.Amount),
                    AccountHistoryType.Withdraw, "Account withdraw", changeTransferLimit);
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(BackOfficeController), "AccountWithdraw", request?.ToJson(), e);
                return Ok(false);
            }

            _consoleWriter.WriteLine($"account withdraw for clientId = {request.ClientId}");
            _operationsLogService.AddLog($"account withdraw {request.PaymentType}", request.ClientId, request.AccountId, request.ToJson(), true.ToJson());

            return Ok(true);
        }

        private bool IsCrypto(string baseAssetId)
        {
            return baseAssetId == LykkeConstants.BitcoinAssetId
                   || baseAssetId == LykkeConstants.LykkeAssetId
                   || baseAssetId == LykkeConstants.EthAssetId
                   || baseAssetId == LykkeConstants.SolarAssetId;
        }

        [Route("marginTradingAccounts/reset")]
        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> AccountWithdrawDepositDemo([FromBody]AccounResetRequest request)
        {
            if (_marginSettings.IsLive)
                return Ok(false);

            await _accountManager.ResetAccountAsync(request.ClientId, request.AccountId);

            _consoleWriter.WriteLine($"account reset for clientId = {request.ClientId}");
            _operationsLogService.AddLog("account reset", request.ClientId, request.AccountId, request.ToJson(), true.ToJson());

            return Ok(true);
        }

        #endregion


        #region Matching engine routes

        [HttpGet]
        [Route("routes")]
        [ProducesResponseType(typeof(List<MatchingEngineRoute>), 200)]
        public IActionResult GetAllRoutes()
        {
            var routes = _routesManager.GetRoutes();
            return Ok(routes);
        }

        [HttpGet]
        [Route("routes/{id}")]
        [ProducesResponseType(typeof(MatchingEngineRoute), 200)]
        public IActionResult GetRoute(string id)
        {
            var route = _routesManager.GetRouteById(id);
            return Ok(route);
        }

        [HttpPost]
        [Route("routes")]
        public async Task<IActionResult> AddRoute([FromBody]NewMatchingEngineRouteRequest request)
        {
            IMatchingEngineRoute newRoute = NewMatchingEngineRouteRequest.CreateRoute(request);
            await _routesManager.AddOrReplaceRouteAsync(newRoute);
            return Ok(newRoute);
        }

        [HttpPut]
        [Route("routes/{id}")]
        public async Task<IActionResult> EditRoute(string id, [FromBody]NewMatchingEngineRouteRequest request)
        {
            var existingRoute = _routesManager.GetRouteById(id);
            if (existingRoute != null)
            {
                var route = NewMatchingEngineRouteRequest.CreateRoute(request, id);
                await _routesManager.AddOrReplaceRouteAsync(route);
                return Ok(_routesManager);
            }
            else
                throw new Exception("MatchingEngine Route not found");
        }

        [HttpDelete]
        [Route("routes/{id}")]
        public async Task<IActionResult> DeleteRoute(string id)
        {
            await _routesManager.DeleteRouteAsync(id);
            return Ok();
        }

        #endregion


        #region Settings

        [HttpGet]
        [Route("settings/enabled/{clientId}")]
        [ProducesResponseType(typeof(bool), 200)]
        [SkipMarginTradingEnabledCheck]
        public async Task<IActionResult> GetMarginTradingIsEnabled(string clientId)
        {
            var settings = await _clientSettingsRepository.GetSettings<MarginEnabledSettings>(clientId);

            if (_marginSettings.IsLive)
                return Ok(settings.EnabledLive);

            return Ok(settings.Enabled);
        }

        [HttpPost]
        [Route("settings/enabled/{clientId}")]
        [SkipMarginTradingEnabledCheck]
        public async Task<IActionResult> SetMarginTradingIsEnabled(string clientId, [FromBody]bool enabled)
        {
            await _marginTradingSettingsService.SetMarginTradingEnabled(clientId, _marginSettings.IsLive, enabled);
            return Ok();
        }

        #endregion


        #region Service

        [HttpPost]
        [Route(LykkeConstants.MaintenanceModeRoute)]
        public IActionResult SetMaintenanceMode([FromBody]bool enabled)
        {
            _maintenanceModeService.SetMode(enabled);

            return Ok();
        }

        [HttpGet]
        [Route(LykkeConstants.MaintenanceModeRoute)]
        public IActionResult GetMaintenanceMode()
        {
            var result = _maintenanceModeService.CheckIsEnabled();

            return Ok(result);
        }

        #endregion
    }
}
