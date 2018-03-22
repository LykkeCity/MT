using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.AccountAssetPair;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/accountAssetPairs")]
    public class AccountAssetPairsController : Controller, IAccountAssetPairsReadingApi
    {
        private readonly IConvertService _convertService;
        private readonly IAccountAssetPairsRepository _accountAssetPairsRepository;

        public AccountAssetPairsController(IAccountAssetPairsRepository accountAssetPairsRepository, IConvertService convertService)
        {
            _accountAssetPairsRepository = accountAssetPairsRepository;
            _convertService = convertService;
        }

        [HttpGet]
        [Route("")]
        public async Task<List<AccountAssetPairContract>> List()
        {
            return (await _accountAssetPairsRepository.GetAllAsync()).Select(Convert).ToList();
        }

        [HttpGet]
        [Route("byAsset/{tradingConditionId}/{baseAssetId}")]
        public async Task<List<AccountAssetPairContract>> Get(string tradingConditionId, string baseAssetId)
        {
            return (await _accountAssetPairsRepository.GetAllAsync(tradingConditionId, baseAssetId)).Select(Convert).ToList();            
        }

        [HttpGet]
        [Route("byAssetPair/{tradingConditionId}/{baseAssetId}/{assetPairId}")]
        public async Task<AccountAssetPairContract> Get(string tradingConditionId, string baseAssetId, string assetPairId)
        {
            return Convert(await _accountAssetPairsRepository.GetAsync(tradingConditionId, baseAssetId, assetPairId)
                           ?? throw new Exception(string.Format(MtMessages.AccountAssetForTradingConditionNotFound,
                               tradingConditionId, baseAssetId, assetPairId)));
        }

        private AccountAssetPairContract Convert(IAccountAssetPair settings)
        {
            return _convertService.Convert<IAccountAssetPair, AccountAssetPairContract>(settings);
        }
    }
}