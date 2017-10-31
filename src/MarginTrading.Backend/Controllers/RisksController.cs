using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Middleware;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/risks")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class RisksController : Controller
    {
        private readonly IAssetPairsCache _instrumentsCache;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly MarginSettings _marginSettings;
        private readonly IMarginTradingAccountHistoryRepository _accountHistoryRepository;

        public RisksController(IAssetPairsCache instrumentsCache,
            IAccountsCacheService accountsCacheService, 
            MarginSettings marginSettings,
            IMarginTradingAccountHistoryRepository accountHistoryRepository)
        {
            _instrumentsCache = instrumentsCache;
            _accountsCacheService = accountsCacheService;
            _marginSettings = marginSettings;
            _accountHistoryRepository = accountHistoryRepository;
        }

        [Route("assets")]
        [HttpGet]
        public AssetPairBackendContract[] GetAllAssets()
        {
            var instruments = _instrumentsCache.GetAll();
            return instruments.Select(item => item.ToBackendContract()).ToArray();
        }

        [Route("accounts")]
        [HttpGet]
        public MarginTradingAccountBackendContract[] GetAllAccounts()
        {
            var accounts = _accountsCacheService.GetAll();
            return accounts.Select(item => item.ToBackendContract(_marginSettings.IsLive)).ToArray();
        }

        [Route("accounts/history")]
        [HttpGet]
        public async Task<Dictionary<string, AccountHistoryBackendContract[]>> GetAllAccountsHistory(
            [FromQuery] string accountId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var accountIds = accountId != null
                ? new[] {accountId}
                : _accountsCacheService.GetAll().Select(item => item.Id).ToArray();

            var history = await _accountHistoryRepository.GetAsync(accountIds, from, to);

            return history.GroupBy(i => i.AccountId)
                .ToDictionary(g => g.Key, g => g.Select(i => i.ToBackendContract()).ToArray());
        }
    }
}
