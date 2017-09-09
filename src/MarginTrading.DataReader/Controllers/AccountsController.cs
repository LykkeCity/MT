using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Core;
using MarginTrading.DataReader.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/accounts")]
    public class AccountsController : Controller
    {
        private readonly MarginSettings _marginSettings;
        private readonly IMarginTradingAccountsRepository _accountsRepository;
        private readonly IMarginTradingAccountStatsRepository _accountStatsRepository;

        public AccountsController(MarginSettings marginSettings, IMarginTradingAccountsRepository accountsRepository,
            IMarginTradingAccountStatsRepository accountStatsRepository)
        {
            _marginSettings = marginSettings;
            _accountsRepository = accountsRepository;
            _accountStatsRepository = accountStatsRepository;
        }

        /// <summary>
        ///     Returns all accounts
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<DataReaderAccountBackendContract>> GetAllAccounts()
        {
            return (await _accountsRepository.GetAllAsync())
                .Select(item => ToBackendContract(item, _marginSettings.IsLive));
        }

        /// <summary>
        ///     Returns all account stats
        /// </summary>
        [HttpGet]
        [Route("stats")]
        public async Task<IEnumerable<MarginTradingAccountStats>> GetAllAccountStats()
        {
            return (await _accountStatsRepository.GetAllAsync()).Select(ToBackendContract);
        }

        /// <summary>
        ///     Returns all accounts by client
        /// </summary>
        [HttpGet]
        [Route("byClient/{clientId}")]
        public async Task<IEnumerable<MarginTradingAccount>> GetAccountsByClientId(string clientId)
        {
            return (await _accountsRepository.GetAllAsync(clientId)).Select(MarginTradingAccount.Create);
        }

        private static DataReaderAccountBackendContract ToBackendContract(IMarginTradingAccount src, bool isLive)
        {
            return new DataReaderAccountBackendContract
            {
                Id = src.Id,
                ClientId = src.ClientId,
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                IsLive = isLive
            };
        }

        private static MarginTradingAccountStats ToBackendContract(IMarginTradingAccountStats item)
        {
            return new MarginTradingAccountStats
            {
                AccountId = item.AccountId,
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
            };
        }
    }
}