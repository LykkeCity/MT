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
    public class AssetPairSettingsController : Controller, IAssetPairSettingsReadingApi
    {
        private readonly IConvertService _convertService;
        private readonly IAssetPairSettingsRepository _assetPairSettingsRepository;

        public AssetPairSettingsController(IConvertService convertService,
            IAssetPairSettingsRepository assetPairSettingsRepository)
        {
            _convertService = convertService;
            _assetPairSettingsRepository = assetPairSettingsRepository;
        }

        /// <summary>
        /// Get all settings
        /// </summary>
        [HttpGet, Route("")]
        public async Task<List<AssetPairSettingsContract>> List()
        {
            return (await _assetPairSettingsRepository.Get()).Select(Convert).ToList();
        }

        /// <summary>
        /// Get settings by id
        /// </summary>
        [HttpGet, Route("{assetPairId}")]
        public async Task<AssetPairSettingsContract> Get(string assetPairId)
        {
            return Convert(await _assetPairSettingsRepository.Get(assetPairId));
        }

        /// <summary>
        /// Get settings by MatchingEngineMode
        /// </summary>
        [HttpGet, Route("by-mode/{matchingEngineMode}")]
        public async Task<List<AssetPairSettingsContract>> Get(MatchingEngineModeContract matchingEngineMode)
        {
            return (await _assetPairSettingsRepository.Get()).Select(Convert)
                .Where(s => s.MatchingEngineMode == matchingEngineMode).ToList();
        }

        private AssetPairSettingsContract Convert(IAssetPairSettings settings)
        {
            return _convertService.Convert<IAssetPairSettings, AssetPairSettingsContract>(settings);
        }
    }
}