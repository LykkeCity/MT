using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Core;
using MarginTrading.Core.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/accountAssets")]
    public class AccountAssetsController : Controller
    {
        private readonly IAccountAssetPairsRepository _accountAssetPairsRepository;

        public AccountAssetsController(IAccountAssetPairsRepository accountAssetPairsRepository)
        {
            _accountAssetPairsRepository = accountAssetPairsRepository;
        }

        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<AccountAssetPair>> GetAll()
        {
            return (await _accountAssetPairsRepository.GetAllAsync()).Select(AccountAssetPair.Create);
        }

        [HttpGet]
        [Route("byAsset/{tradingConditionId}/{baseAssetId}")]
        public async Task<IEnumerable<AccountAssetPair>> GetByAsset(string tradingConditionId, string baseAssetId)
        {
            return (await _accountAssetPairsRepository.GetAllAsync(tradingConditionId, baseAssetId)).Select(AccountAssetPair.Create);
        }

        [HttpGet]
        [Route("byAssetPair/{tradingConditionId}/{baseAssetId}/{assetPairId}")]
        public async Task<AccountAssetPair> GetByAssetPairId(string tradingConditionId, string baseAssetId, string assetPairId)
        {
            var accountAsset = await _accountAssetPairsRepository.GetAsync(tradingConditionId, baseAssetId, assetPairId)
                   ?? throw new Exception(string.Format(MtMessages.AccountAssetForTradingConditionNotFound,
                       tradingConditionId, baseAssetId, assetPairId));
            return AccountAssetPair.Create(accountAsset);
        }
    }
}