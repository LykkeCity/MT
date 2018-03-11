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

        public DictionariesController(IAssetsServiceWithCache assetsService)
        {
            _assetsService = assetsService;
        }

        [HttpGet]
        [Route("assetPairs")]
        public async Task<IEnumerable<AssetPair>> GetAllAssetPairs()
        {
            return (await _assetsService.GetAllAssetPairsAsync()).Select(a =>
                new AssetPair(a.Id, a.Name, a.BaseAssetId, a.QuotingAssetId, a.Accuracy));
        }

        [HttpGet]
        [Route("matchingEngines")]
        public string[] GetAllMatchingEngines()
        {
            //TODO: replace by Ids when ME infos will be stored in DB
            return new[]
            {
                MatchingEngineConstants.LykkeVuMm,
                MatchingEngineConstants.LykkeCyStp,
                MatchingEngineConstants.Reject
            };
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