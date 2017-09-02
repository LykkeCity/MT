using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/accountGroups")]
    public class AccountGroupsController : Controller
    {
        private readonly IMarginTradingAccountGroupRepository _accountGroupRepository;

        public AccountGroupsController(IMarginTradingAccountGroupRepository accountGroupRepository)
        {
            _accountGroupRepository = accountGroupRepository;
        }


        /// <summary>
        /// Returns all account groups
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<MarginTradingAccountGroup>> GetAllAccountGroups()
        {
            return (await _accountGroupRepository.GetAllAsync()).Select(MarginTradingAccountGroup.Create);
        }

        /// <summary>
        /// Returns an account groups by <paramref name="tradingConditionId"/> and <paramref name="baseAssetId"/>
        /// </summary>
        [HttpGet]
        [Route("byBaseAsset/{tradingConditionId}/{baseAssetId}")]
        public async Task<MarginTradingAccountGroup> GetAccountGroup(string tradingConditionId, string baseAssetId)
        {
            return MarginTradingAccountGroup.Create(await _accountGroupRepository.GetAsync(tradingConditionId, baseAssetId));
        }

    }
}
