using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Common.BackendContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using MarginTrading.Core;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/accounts")]
    public class AccountsController : Controller
    {
        private readonly Settings.MarginSettings _marginSettings;
        private readonly IMarginTradingAccountsRepository _accountsRepository;

        public AccountsController(Settings.MarginSettings marginSettings, IMarginTradingAccountsRepository accountsRepository)
        {
            _marginSettings = marginSettings;
            _accountsRepository = accountsRepository;
        }

        /// <summary>
        /// Returns all accounts
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("")]
        public async Task<DataReaderAccountBackendContract[]> GetAllAccounts()
        {
            return (await _accountsRepository.GetAllAsync())
                .Select(item => ToBackendContract(item, _marginSettings.IsLive)).ToArray();
        }

        /// <summary>
        /// Returns all accounts by client
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
    }
}