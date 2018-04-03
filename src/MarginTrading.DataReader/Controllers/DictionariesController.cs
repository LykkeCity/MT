using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.AssetPairSettings;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/dictionaries")]
    public class DictionariesController : Controller, IDictionariesReadingApi
    {
        private readonly IAssetPairsRepository _assetPairsRepository;
        private readonly IConvertService _convertService;

        public DictionariesController(IAssetPairsRepository assetPairsRepository, IConvertService convertService)
        {
            _assetPairsRepository = assetPairsRepository;
            _convertService = convertService;
        }

        [HttpGet]
        [Route("assetPairs")]
        public async Task<List<AssetPairContract>> AssetPairs()
        {
            return (await _assetPairsRepository.GetAsync()).Select(Convert).ToList();
        }

        private AssetPairContract Convert(IAssetPair assetPair)
        {
            return _convertService.Convert<IAssetPair, AssetPairContract>(assetPair);
        }

        [HttpGet]
        [Route("matchingEngines")]
        public Task<List<string>> MatchingEngines()
        {
            //TODO: replace by Ids when ME infos will be stored in DB
            return Task.FromResult((new[]
            {
                MatchingEngineConstants.LykkeVuMm,
                MatchingEngineConstants.LykkeCyStp,
                MatchingEngineConstants.Reject
            }).ToList());
        }

        [HttpGet]
        [Route("orderTypes")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public Task<List<string>> OrderTypes()
        {            
            return Task.FromResult(Enum.GetNames(typeof(OrderDirection)).ToList());
        }
    }
}