using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AzureRepositories;
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
        private readonly IMarginTradingAccountStatsRepository _accountStatsRepository;

        public AccountsController(IAccountsCacheService accountsCacheService,
            IDateService dateService,
            AccountManager accountManager,
            TradingConditionsCacheService tradingConditionsCache, 
            IMarginTradingAccountStatsRepository accountStatsRepository)
        {
            _accountsCacheService = accountsCacheService;
            _dateService = dateService;
            _accountManager = accountManager;
            _tradingConditionsCache = tradingConditionsCache;
            _accountStatsRepository = accountStatsRepository;
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
        /// <param name="accountId"></param>
        [HttpGet]
        [Route("stats")]
        public async Task<List<AccountStatContract>> GetAllAccountStats(string accountId = null)
        {
            var stats = await _accountStatsRepository.GetAllAsync();
            if (accountId != null)
                stats = stats.Where(s => s.AccountId == accountId);
            
            return stats.Select(Convert).ToList();
        }
        
        private static AccountStatContract Convert(IMarginTradingAccountStats item)
        {
            return new AccountStatContract
            {
                AccountId = item.AccountId,
                BaseAssetId = item.BaseAssetId,
                MarginCall = item.MarginCall,
                StopOut = item.StopOut,
                TotalCapital = item.TotalCapital,
                FreeMargin = item.FreeMargin,
                MarginAvailable = item.MarginAvailable,
                UsedMargin = item.UsedMargin,
                MarginInit = item.MarginInit,
                PnL = item.PnL,
                OpenPositionsCount = item.OpenPositionsCount,
                MarginUsageLevel = item.MarginUsageLevel,
                LegalEntity = item.LegalEntity,
            };
        }
    }
}