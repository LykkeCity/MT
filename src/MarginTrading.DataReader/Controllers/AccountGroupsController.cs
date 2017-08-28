using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public Task<IEnumerable<IMarginTradingAccountGroup>> GetAll()
        {
            return _accountGroupRepository.GetAllAsync();
        }

        /// <summary>
        /// Returns an account groups by <paramref name="tradingConditionId"/> and <paramref name="baseAssetId"/>
        /// </summary>
        /// <param name="tradingConditionId"></param>
        /// <param name="baseAssetId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("byBaseAsset/{tradingConditionId}/{baseAssetId}")]
        public Task<IMarginTradingAccountGroup> Get(string tradingConditionId, string baseAssetId)
        {
            return _accountGroupRepository.GetAsync(tradingConditionId, baseAssetId);
        }

    }
}
