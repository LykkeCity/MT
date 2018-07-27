using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.AssetSettings;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Common.Middleware;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Route("api/[controller]"), Authorize, MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class AssetController : Controller, IAssetEditingApi
    {
        private readonly IConvertService _convertService;
        private readonly IAssetManager _assetManager;
  
        public AssetController(IConvertService convertService, IAssetManager assetManager)
        {
            _convertService = convertService;
            _assetManager = assetManager;
        }

        /// <summary>
        /// Insert new Asset
        /// </summary>
        [HttpPost, Route("{assetId}")]
        public async Task<AssetContract> Insert(string assetId,
            [FromBody] AssetInputContract settings)
        {
            return Convert(await _assetManager.InsertAsset(Convert(assetId, settings)));
        }

        /// <summary>
        /// Update existing asset
        /// </summary>
        [HttpPut, Route("{assetId}")]
        public async Task<AssetContract> Update(string assetId,
            [FromBody] AssetInputContract settings)
        {
            return Convert(await _assetManager.UpdateAsset(Convert(assetId, settings)));
        }

        /// <summary>
        /// Delete existing asset
        /// </summary>
        [HttpDelete, Route("{assetId}")]
        public async Task<AssetContract> Delete(string assetId)
        {
            return Convert(await _assetManager.DeleteAsset(assetId));
        }

        private IAsset Convert(string assetId, AssetInputContract settings)
        {
            return _convertService.ConvertWithConstructorArgs<AssetInputContract, Asset>(
                settings, new { id = assetId });
        }

        private AssetContract Convert(IAsset settings)
        {
            return _convertService.Convert<IAsset, AssetContract>(settings);
        }
    }
}