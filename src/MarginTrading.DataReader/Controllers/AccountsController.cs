using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using MarginTrading.Common.Mappers;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/accounts")]
    public class AccountsController : Controller
    {
        private readonly MarginSettings _marginSettings;
        private readonly IMarginTradingAccountsRepository _accountsRepository;

        public AccountsController(MarginSettings marginSettings, IMarginTradingAccountsRepository accountsRepository)
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
        public async Task<MarginTradingAccountBackendContract[]> GetAllAccounts()
        {
            return (await _accountsRepository.GetAllAsync())
                .Select(item => item.ToBackendContract(_marginSettings.IsLive)).ToArray();
        }

        /// <summary>
        /// Returns all accounts by client
        /// </summary>
        [HttpGet]
        [Route("byClient/{clientId}")]
        public async Task<IEnumerable<MarginTradingAccount>> GetAccountsByClient(string clientId)
        {
            return (await _accountsRepository.GetAllAsync(clientId)).Select(MarginTradingAccount.Create);
        }
    }
}