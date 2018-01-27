using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Assets.Client;
using MarginTrading.Backend.Core;
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
            return MatchingEngineConstants.All;
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