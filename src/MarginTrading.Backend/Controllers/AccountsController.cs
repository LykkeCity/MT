using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Orders;
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
        /// Returns all account stats
        /// </summary>
        [HttpGet]
        [Route("stats")]
        public Task<List<AccountStatContract>> GetAllAccountStats()
        {
            var stats = _accountsCacheService.GetAll();

            return Task.FromResult(stats.Select(Convert).ToList());
        }

        /// <summary>
        /// Returns all accounts stats, optionally paginated. Both skip and take must be set or unset.
        /// </summary>
        [HttpGet]
        [Route("stats/by-pages")]
        public Task<PaginatedResponseContract<AccountStatContract>> GetAllAccountStatsByPages(
            int? skip = null, int? take = null)
        {
            if ((skip.HasValue && !take.HasValue) || (!skip.HasValue && take.HasValue))
            {
                throw new ArgumentOutOfRangeException(nameof(skip), "Both skip and take must be set or unset");
            }

            if (take.HasValue && (take <= 0 || skip < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be >= 0, take must be > 0");
            }
            
            var stats = _accountsCacheService.GetAllByPages(skip, take);

            return Task.FromResult(Convert(stats));
        }

        /// <summary>
        /// Returns stats of selected account
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
                MarginCallLevel = item.GetMarginCall1Level(),
                StopOutLevel = item.GetStopOutLevel(),
                TotalCapital = item.GetTotalCapital(),
                FreeMargin = item.GetFreeMargin(),
                MarginAvailable = item.GetMarginAvailable(),
                UsedMargin = item.GetUsedMargin(),
                MarginInit = item.GetMarginInit(),
                PnL = item.GetPnl(),
                UnrealizedDailyPnl = item.GetUnrealizedDailyPnl(),
                OpenPositionsCount = item.GetOpenPositionsCount(),
                MarginUsageLevel = item.GetMarginUsageLevel(),
                LegalEntity = item.LegalEntity
            };
        }

        private PaginatedResponseContract<AccountStatContract> Convert(PaginatedResponse<MarginTradingAccount> accounts)
        {
            return new PaginatedResponseContract<AccountStatContract>(
                contents: accounts.Contents.Select(Convert).ToList(),
                start: accounts.Start,
                size: accounts.Size,
                totalSize: accounts.TotalSize
            );
        }
    }
}