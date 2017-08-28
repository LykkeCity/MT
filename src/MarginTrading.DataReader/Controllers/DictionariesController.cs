using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/dictionaries")]
    public class DictionariesController : Controller
    {
        private readonly IMarginTradingAssetPairsRepository _assetPairsRepository;

        public DictionariesController(IMarginTradingAssetPairsRepository assetPairsRepository)
        {
            _assetPairsRepository = assetPairsRepository;
        }

        [HttpGet]
        [Route("assetPairs")]
        public Task<IEnumerable<IMarginTradingAssetPair>> GetAllAssetPairs()
        {
            return _assetPairsRepository.GetAllAsync();
        }

        [HttpGet]
        [Route("matchingEngines")]
        public string[] GetAllMatchingEngines()
        {
            return MatchingEngines.All;
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