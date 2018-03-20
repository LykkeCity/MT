using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AzureRepositories;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.DataReader.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/accounts")]
    public class AccountsController : Controller, IAccountsApi
    {
        private readonly DataReaderSettings _dataReaderSettings;
        private readonly IMarginTradingAccountsRepository _accountsRepository;
        private readonly IMarginTradingAccountStatsRepository _accountStatsRepository;

        public AccountsController(DataReaderSettings dataReaderSettings, IMarginTradingAccountsRepository accountsRepository,
            IMarginTradingAccountStatsRepository accountStatsRepository)
        {
            _dataReaderSettings = dataReaderSettings;
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
                .Select(item => ToBackendContract(item, _dataReaderSettings.IsLive));
        }

        /// <summary>
        ///     Returns all account stats
        /// </summary>
        [HttpGet]
        [Route("stats")]
        public async Task<IEnumerable<DataReaderAccountStatsBackendContract>> GetAllAccountStats()
        {
            return (await _accountStatsRepository.GetAllAsync()).Select(ToBackendContract);
        }

        /// <summary>
        /// Returns all accounts by client
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("byClient/{clientId}")]
        public async Task<IEnumerable<DataReaderAccountBackendContract>> GetAccountsByClientId(string clientId)
        {
            return (await _accountsRepository.GetAllAsync(clientId))
                .Select(x => ToBackendContract(MarginTradingAccount.Create(x), _dataReaderSettings.IsLive));
        }

        /// <summary>
        /// Returns account by it's Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="AccountNotFoundException"></exception>
        [HttpGet]
        [Route("byId/{id}")]
        public async Task<DataReaderAccountBackendContract> GetAccountById(string id)
        {
            var account = await _accountsRepository.GetAsync(id);

            if (account == null)
                throw new AccountNotFoundException(id, "Account was not found.");
            
            return ToBackendContract(MarginTradingAccount.Create(account), _dataReaderSettings.IsLive);
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
                IsLive = isLive,
                LegalEntity = src.LegalEntity,
            };
        }
        
        private static DataReaderAccountStatsBackendContract ToBackendContract(IMarginTradingAccountStats item)
        {
            return new DataReaderAccountStatsBackendContract
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