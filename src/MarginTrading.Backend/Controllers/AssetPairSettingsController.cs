using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.AssetPairSettings;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Common.Middleware;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Route("api/[controller]"), Authorize, MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class AssetPairSettingsController : Controller, IAssetPairSettingsEditingApi
    {
        private readonly IConvertService _convertService;
        private readonly IAssetPairsManager _assetPairsManager;

        public AssetPairSettingsController(IConvertService convertService, IAssetPairsManager assetPairsManager)
        {
            _convertService = convertService;
            _assetPairsManager = assetPairsManager;
        }

        /// <summary>
        /// Insert new settings for a pair
        /// </summary>
        [HttpPost, Route("{assetPairId}")]
        public async Task<AssetPairSettingsContract> Insert(string assetPairId,
            [FromBody] AssetPairSettingsInputContract settings)
        {
            return Convert(await _assetPairsManager.InsertAssetPairSettings(Convert(assetPairId, settings)));
        }

        /// <summary>
        /// Update existing settings for a pair
        /// </summary>
        [HttpPut, Route("{assetPairId}")]
        public async Task<AssetPairSettingsContract> Update(string assetPairId,
            [FromBody] AssetPairSettingsInputContract settings)
        {
            return Convert(await _assetPairsManager.UpdateAssetPairSettings(Convert(assetPairId, settings)));
        }

        /// <summary>
        /// Delete existing settings for a pair
        /// </summary>
        [HttpDelete, Route("{assetPairId}")]
        public async Task<AssetPairSettingsContract> Delete(string assetPairId)
        {
            return Convert(await _assetPairsManager.DeleteAssetPairSettings(assetPairId));
        }

        private IAssetPairSettings Convert(string assetPairId, AssetPairSettingsInputContract settings)
        {
            return _convertService.ConvertWithConstructorArgs<AssetPairSettingsInputContract, AssetPairSettings>(
                settings, new {assetPairId});
        }

        private AssetPairSettingsContract Convert(IAssetPairSettings settings)
        {
            return _convertService.Convert<IAssetPairSettings, AssetPairSettingsContract>(settings);
        }
    }
}