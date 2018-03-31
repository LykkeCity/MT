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
    [Route("api/[controller]"), Authorize]
    public class AssetPairsController : Controller, IAssetPairsReadingApi
    {
        private readonly IConvertService _convertService;
        private readonly IAssetPairsRepository _assetPairsRepository;

        public AssetPairsController(IConvertService convertService,
            IAssetPairsRepository assetPairsRepository)
        {
            _convertService = convertService;
            _assetPairsRepository = assetPairsRepository;
        }

        /// <summary>
        /// Get all pairs
        /// </summary>
        [HttpGet, Route("")]
        public async Task<List<AssetPairContract>> List()
        {
            return (await _assetPairsRepository.GetAsync()).Select(Convert).ToList();
        }

        /// <summary>
        /// Get pair by id
        /// </summary>
        [HttpGet, Route("{assetPairId}")]
        public async Task<AssetPairContract> Get(string assetPairId)
        {
            return Convert(await _assetPairsRepository.GetAsync(assetPairId));
        }

        /// <summary>
        /// Get pairs by LegalEntity and MatchingEngineMode
        /// </summary>
        [HttpGet, Route("{legalEntity}/{matchingEngineMode}")]
        public async Task<List<AssetPairContract>> Get(string legalEntity, MatchingEngineModeContract matchingEngineMode)
        {
            return (await _assetPairsRepository.GetAsync()).Select(Convert)
                .Where(s => s.MatchingEngineMode == matchingEngineMode && s.LegalEntity == legalEntity).ToList();
        }

        private AssetPairContract Convert(IAssetPair settings)
        {
            return _convertService.Convert<IAssetPair, AssetPairContract>(settings);
        }
    }
}