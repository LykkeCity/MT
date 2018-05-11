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

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/accounts-management")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class AccountsManagementController : Controller
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IDateService _dateService;
        private readonly AccountManager _accountManager;
        private readonly AccountGroupCacheService _accountGroupCacheService;

        public AccountsManagementController(IAccountsCacheService accountsCacheService,
            IDateService dateService,
            AccountManager accountManager,
            AccountGroupCacheService accountGroupCacheService)
        {
            _accountsCacheService = accountsCacheService;
            _dateService = dateService;
            _accountManager = accountManager;
            _accountGroupCacheService = accountGroupCacheService;
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
    }
}