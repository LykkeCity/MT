using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using MarginTrading.Services;
using MarginTrading.Services.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/accountprofile")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class AccountProfileController : Controller
    {
        private readonly IMarginTradingAccountHistoryRepository _accountsHistoryRepository;
        private readonly IMarginTradingOrdersHistoryRepository _ordersHistoryRepository;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly OrdersCache _ordersCache;
        private readonly MarginSettings _settings;

        public AccountProfileController(
            IMarginTradingAccountHistoryRepository accountsHistoryRepository,
            IMarginTradingOrdersHistoryRepository ordersHistoryRepository,
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            MarginSettings settings)
        {
            _accountsHistoryRepository = accountsHistoryRepository;
            _ordersHistoryRepository = ordersHistoryRepository;
            _accountsCacheService = accountsCacheService;
            _ordersCache = ordersCache;
            _settings = settings;
        }

        /// <summary>
        /// Returns margin account by id
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("account/{clientId}/{accountId}")]
        public MarginTradingAccountBackendContract GetAccount(string clientId, string accountId)
        {
            return _accountsCacheService.Get(clientId, accountId).ToBackendContract(_settings.IsLive);
        }

        /// <summary>
        /// Returns account open positions by account id
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("openPositions/{accountId}")]
        public IEnumerable<OrderContract> GetAccountOrders(string accountId)
        {
            return _ordersCache.ActiveOrders.GetOrdersByAccountIds(accountId).Select(item => item.ToBaseContract());
        }

        /// <summary>
        /// Returns account history by account id
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("history/{clientId}/{accountId}")]
        public async Task<AccountHistoryBackendResponse> GetAccountHistory(string clientId, string accountId)
        {
            var now = DateTime.UtcNow;

            var accounts = (await _accountsHistoryRepository.GetAsync(new[] { accountId }, now.AddYears(-1), now))
                .Where(item => item.Type != AccountHistoryType.OrderClosed)
                .OrderByDescending(item => item.Date);

            var historyOrders = (await _ordersHistoryRepository.GetHistoryAsync(clientId, new[] { accountId }, now.AddYears(-1), now))
                .Where(item => item.Status != OrderStatus.Rejected);

            var openPositions = _ordersCache.ActiveOrders.GetOrdersByAccountIds(accountId);

            return AccountHistoryBackendResponse.Create(accounts, openPositions, historyOrders);
        }
    }
}
