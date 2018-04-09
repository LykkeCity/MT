using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.TradingConditions;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/accountGroups")]
    public class AccountGroupsController : Controller, IAccountGroupsReadingApi
    {
        private readonly IAccountGroupRepository _accountGroupRepository;
        private readonly IConvertService _convertService;

        public AccountGroupsController(IAccountGroupRepository accountGroupRepository, IConvertService convertService)
        {
            _accountGroupRepository = accountGroupRepository;
            _convertService = convertService;
        }


        /// <summary>
        /// Returns all account groups
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("")]
        public async Task<List<AccountGroupContract>> List()
        {
            return (await _accountGroupRepository.GetAllAsync())
                .Select(Convert)
                .ToList();
        }

        /// <summary>
        /// Returns an account groups by <paramref name="tradingConditionId"/> and <paramref name="baseAssetId"/>
        /// </summary>
        [HttpGet]
        [Route("byBaseAsset/{tradingConditionId}/{baseAssetId}")]
        [ProducesResponseType(typeof(AccountGroup), 200)]
        [ProducesResponseType(typeof(AccountGroup), 204)]
        public async Task<AccountGroupContract> GetByBaseAsset(string tradingConditionId, string baseAssetId)
        {
            var accountGroup = await _accountGroupRepository.GetAsync(tradingConditionId, baseAssetId);

            return accountGroup == null ? null : Convert(accountGroup);
        }

        private AccountGroupContract Convert(IAccountGroup src)
        {
            return _convertService.Convert<IAccountGroup, AccountGroupContract>(src);
        }
    }
}
