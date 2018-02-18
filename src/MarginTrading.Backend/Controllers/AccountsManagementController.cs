using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Middleware;
using MarginTrading.Common.Services;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.Contract.BackendContracts.AccountsManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class AccountsManagementController : Controller
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IDateService _dateService;
        private readonly AccountManager _accountManager;
        private readonly AccountGroupCacheService _accountGroupCacheService;
        private readonly ITradingConditionsCacheService _tradingConditionsCacheService;

        public AccountsManagementController(IAccountsCacheService accountsCacheService,
            IDateService dateService,
            AccountManager accountManager,
            AccountGroupCacheService accountGroupCacheService,
            ITradingConditionsCacheService tradingConditionsCacheService)
        {
            _accountsCacheService = accountsCacheService;
            _dateService = dateService;
            _accountManager = accountManager;
            _accountGroupCacheService = accountGroupCacheService;
            _tradingConditionsCacheService = tradingConditionsCacheService;
        }
        
        
        /// <summary>
        /// Get all accounts where (balance + pnl) / Used margin less or equal than threshold value
        /// </summary>
        /// <param name="threshold">Minimal margin usege level</param>
        [ProducesResponseType(typeof(AccountsMarginLevelResponse), 200)]
        [Route("marginLevels/{threshold:decimal}")]
        [HttpGet]
        public AccountsMarginLevelResponse GetAccountsMarginLevels(decimal threshold)
        {
            var accounts = _accountsCacheService.GetAll()
                .Select(a =>
                    new AccountsMarginLevelContract
                    {
                        AccountId = a.Id,
                        ClientId = a.ClientId,
                        TradingConditionId = a.TradingConditionId,
                        BaseAssetId = a.BaseAssetId,
                        Balance = a.Balance,
                        MarginLevel = a.GetMarginUsageLevel(),
                        OpenedPositionsCount = a.GetOpenPositionsCount(),
                        UsedMargin = a.GetUsedMargin(),
                        TotalBalance = a.GetTotalCapital()
                    })
                .Where(a => a.MarginLevel <= threshold)
                .ToArray();

            return new AccountsMarginLevelResponse
            {
                DateTime = _dateService.Now(),
                Levels = accounts
            };
        }

        /// <summary>
        /// Close positions for accounts
        /// </summary>
        [ProducesResponseType(typeof(CloseAccountPositionsResponse), 200)]
        [Route("closePositions")]
        [HttpPost]
        public async Task<CloseAccountPositionsResponse> GetAccountsMarginLevels([FromBody] CloseAccountPositionsRequest request)
        {
            request.RequiredNotNull(nameof(request));
            
            var accounts = request.IgnoreMarginLevel
                ? null
                : _accountsCacheService.GetAll().ToDictionary(a => a.Id);
            
            var result = new CloseAccountPositionsResponse()
            {
                Results = new List<CloseAccountPositionsResult>()
            };
            
            foreach (var accountId in request.AccountIds)    
            {
                if (!request.IgnoreMarginLevel)
                {
                    var account = accounts[accountId];
                    var accountGroup =
                        _accountGroupCacheService.GetAccountGroup(account.TradingConditionId, account.BaseAssetId);
                    var accountMarginUsageLevel = account.GetMarginUsageLevel();

                    if (accountMarginUsageLevel > accountGroup.MarginCall)
                    {
                        result.Results.Add(new CloseAccountPositionsResult
                        {
                            AccountId = accountId,
                            ClosedPositions = new OrderFullContract[0],
                            ErrorMessage =
                                $"Account margin usage level [{accountMarginUsageLevel}] is greater then margin call level [{accountGroup.MarginCall}]"
                        });

                        continue;
                    }
                }

                var closedOrders = await _accountManager.CloseAccountOrders(accountId, OrderCloseReason.ClosedByBroker);
                
                result.Results.Add(new CloseAccountPositionsResult
                {
                    AccountId = accountId,
                    ClosedPositions = closedOrders.Select(o => o.ToFullContract()).ToArray()
                });
            }

            return result;
        }

        /// <summary>
        /// Sets trading condition for account
        /// </summary>
        /// <response code="200">Returns changed account</response>
        [HttpPost]
        [Route("tradingCondition")]
        [SwaggerOperation("SetTradingCondition")]
        public async Task<MtBackendResponse<MarginTradingAccountModel>> SetTradingCondition(
            [FromBody] SetTradingConditionModel model)
        {
            if (!_tradingConditionsCacheService.IsTradingConditionExists(model.TradingConditionId))
            {
                return MtBackendResponse<MarginTradingAccountModel>.Error(
                    $"No trading condition {model.TradingConditionId} found in cache");
            }

            var account =
                await _accountManager.SetTradingCondition(model.ClientId, model.AccountId, model.TradingConditionId);

            if (account == null)
                return MtBackendResponse<MarginTradingAccountModel>.Error(
                    $"Account for client [{model.ClientId}] with id [{model.AccountId}] was not found");

            return MtBackendResponse<MarginTradingAccountModel>.Ok(account.ToBackendContract());
        }

        /// <summary>
        /// Create accounts with requested base asset for all users 
        /// that already have accounts with requested trading condition
        /// </summary>
        [HttpPost]
        [Route("accountGroup/init")]
        [SwaggerOperation("InitAccountGroup")]
        public async Task<MtBackendResponse<IEnumerable<MarginTradingAccountModel>>> InitAccountGroup(
            [FromBody] InitAccountGroupRequest request)
        {
            var tradingCondition = _tradingConditionsCacheService.GetTradingCondition(request.TradingConditionId);

            if (tradingCondition == null)
            {
                return MtBackendResponse<IEnumerable<MarginTradingAccountModel>>.Error(
                    $"No trading condition {request.TradingConditionId} found in cache");
            }

            var accountGroup =
                _accountGroupCacheService.GetAccountGroup(request.TradingConditionId, request.BaseAssetId);

            if (accountGroup == null)
            {
                return MtBackendResponse<IEnumerable<MarginTradingAccountModel>>.Error(
                    $"No account group {request.TradingConditionId}_{request.BaseAssetId} found in cache");
            }

            var newAccounts = await _accountManager.CreateAccounts(request.TradingConditionId, request.BaseAssetId);

            return MtBackendResponse<IEnumerable<MarginTradingAccountModel>>.Ok(
                newAccounts.Select(a => a.ToBackendContract()));
        }
        
    }
}