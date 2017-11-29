using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/accountGroups")]
    public class AccountGroupsController : Controller
    {
        private readonly IAccountGroupRepository _accountGroupRepository;

        public AccountGroupsController(IAccountGroupRepository accountGroupRepository)
        {
            _accountGroupRepository = accountGroupRepository;
        }


        /// <summary>
        /// Returns all account groups
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<AccountGroup>> GetAllAccountGroups()
        {
            return (await _accountGroupRepository.GetAllAsync()).Select(AccountGroup.Create);
        }

        /// <summary>
        /// Returns an account groups by <paramref name="tradingConditionId"/> and <paramref name="baseAssetId"/>
        /// </summary>
        [HttpGet]
        [Route("byBaseAsset/{tradingConditionId}/{baseAssetId}")]
        [ProducesResponseType(typeof(AccountGroup), 200)]
        [ProducesResponseType(typeof(AccountGroup), 204)]
        public async Task<AccountGroup> GetAccountGroup(string tradingConditionId, string baseAssetId)
        {
            var accountGroup = await _accountGroupRepository.GetAsync(tradingConditionId, baseAssetId);

            return accountGroup == null ? null : AccountGroup.Create(accountGroup);
        }

    }
}
