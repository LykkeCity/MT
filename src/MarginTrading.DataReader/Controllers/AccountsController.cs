using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AzureRepositories;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/accounts")]
    public class AccountsController : Controller, IAccountsApi
    {
        private readonly IMarginTradingAccountStatsRepository _accountStatsRepository;

        public AccountsController(IMarginTradingAccountStatsRepository accountStatsRepository)
        {
            _accountStatsRepository = accountStatsRepository;
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