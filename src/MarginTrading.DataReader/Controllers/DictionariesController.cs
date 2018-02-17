using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Assets.Client;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/dictionaries")]
    public class DictionariesController : Controller
    {
        private readonly IAssetsServiceWithCache _assetsService;
        private readonly IMatchingEngineRepository _matchingEngineRepository;

        public DictionariesController(IAssetsServiceWithCache assetsService,
            IMatchingEngineRepository matchingEngineRepository)
        {
            _assetsService = assetsService;
            _matchingEngineRepository = matchingEngineRepository;
        }

        [HttpGet]
        [Route("assetPairs")]
        public async Task<IEnumerable<AssetPair>> GetAllAssetPairs()
        {
            return (await _assetsService.GetAllAssetPairsAsync()).Select(a => new AssetPair
            {
                Id = a.Id,
                Name = a.Name,
                Accuracy = a.Accuracy,
                BaseAssetId = a.BaseAssetId,
                QuoteAssetId = a.QuotingAssetId
            });
        }

        [HttpGet]
        [Route("matchingEngines")]
        public string[] GetAllMatchingEngines()
        {
            return _matchingEngineRepository.GetMatchingEngines().Select(me => me.Id).ToArray();
        }

        [HttpGet]
        [Route("orderTypes")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public string[] GetAllOrderTypes()
        {
            return Enum.GetNames(typeof(OrderDirection));
        }
    }
}