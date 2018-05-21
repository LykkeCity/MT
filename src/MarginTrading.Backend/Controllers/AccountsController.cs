using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Account;
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
using IAccountsApi = MarginTrading.Backend.Contracts.IAccountsApi;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/accounts")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class AccountsController : Controller, IAccountsApi
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IDateService _dateService;
        private readonly AccountManager _accountManager;
        private readonly TradingConditionsCacheService _tradingConditionsCache;

        public AccountsController(IAccountsCacheService accountsCacheService,
            IDateService dateService,
            AccountManager accountManager,
            TradingConditionsCacheService tradingConditionsCache)
        {
            _accountsCacheService = accountsCacheService;
            _dateService = dateService;
            _accountManager = accountManager;
            _tradingConditionsCache = tradingConditionsCache;
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
        public async Task<CloseAccountPositionsResponse> CloseAccountPositions(
            [FromBody] CloseAccountPositionsRequest request)
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
                    var tradingCondition =
                        _tradingConditionsCache.GetTradingCondition(account.TradingConditionId);
                    var accountMarginUsageLevel = account.GetMarginUsageLevel();

                    if (accountMarginUsageLevel > tradingCondition.MarginCall1)
                    {
                        result.Results.Add(new CloseAccountPositionsResult
                        {
                            AccountId = accountId,
                            ClosedPositions = new OrderFullContract[0],
                            ErrorMessage =
                                $"Account margin usage level [{accountMarginUsageLevel}] is greater then margin call level [{tradingCondition.MarginCall1}]"
                        });

                        continue;
                    }
                }

                var closedOrders = await _accountManager.CloseAccountOrders(accountId);

                result.Results.Add(new CloseAccountPositionsResult
                {
                    AccountId = accountId,
                    ClosedPositions = closedOrders.Select(o =>
                    {
                        var orderUpdateType = o.Status == OrderStatus.Closing
                            ? OrderUpdateType.Closing
                            : OrderUpdateType.Close;
                        return o.ToFullContract(orderUpdateType, _dateService.Now());
                    }).ToArray()
                });
            }

            return result;
        }

        /// <summary>
        ///     Returns all account stats
        /// </summary>
        [HttpGet]
        [Route("stats")]
        public Task<List<AccountStatContract>> GetAllAccountStats()
        {
            var stats = _accountsCacheService.GetAll();

            return Task.FromResult(stats.Select(Convert).ToList());
        }
        
        /// <summary>
        ///     Returns stats of selected account
        /// </summary>
        /// <param name="accountId"></param>
        [HttpGet]
        [Route("stats/{accountId}")]
        public Task<AccountStatContract> GetAccountStats(string accountId)
        {
            var stats = _accountsCacheService.Get(accountId);

            return Task.FromResult(Convert(stats));
        }
        
        private static AccountStatContract Convert(IMarginTradingAccount item)
        {
            return new AccountStatContract
            {
                AccountId = item.Id,
                BaseAssetId = item.BaseAssetId,
                Balance = item.Balance,
                MarginCallLevel = item.GetMarginCallLevel(),
                StopOutLevel = item.GetStopOutLevel(),
                TotalCapital = item.GetTotalCapital(),
                FreeMargin = item.GetFreeMargin(),
                MarginAvailable = item.GetMarginAvailable(),
                UsedMargin = item.GetUsedMargin(),
                MarginInit = item.GetMarginInit(),
                PnL = item.GetPnl(),
                OpenPositionsCount = item.GetOpenPositionsCount(),
                MarginUsageLevel = item.GetMarginUsageLevel(),
                LegalEntity = item.LegalEntity
            };
        }
    }
}