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
    public class AssetPairsController : Controller, IAssetPairsEditingApi
    {
        private readonly IConvertService _convertService;
        private readonly IAssetPairsManager _assetPairsManager;

        public AssetPairsController(IConvertService convertService, IAssetPairsManager assetPairsManager)
        {
            _convertService = convertService;
            _assetPairsManager = assetPairsManager;
        }

        /// <summary>
        /// Insert new pair
        /// </summary>
        [HttpPost, Route("{assetPairId}")]
        public async Task<AssetPairContract> Insert(string assetPairId,
            [FromBody] AssetPairInputContract settings)
        {
            return Convert(await _assetPairsManager.InsertAssetPair(Convert(assetPairId, settings)));
        }

        /// <summary>
        /// Update existing pair
        /// </summary>
        [HttpPut, Route("{assetPairId}")]
        public async Task<AssetPairContract> Update(string assetPairId,
            [FromBody] AssetPairInputContract settings)
        {
            return Convert(await _assetPairsManager.UpdateAssetPair(Convert(assetPairId, settings)));
        }

        /// <summary>
        /// Delete existing pair
        /// </summary>
        [HttpDelete, Route("{assetPairId}")]
        public async Task<AssetPairContract> Delete(string assetPairId)
        {
            return Convert(await _assetPairsManager.DeleteAssetPair(assetPairId));
        }

        private IAssetPair Convert(string assetPairId, AssetPairInputContract settings)
        {
            return _convertService.ConvertWithConstructorArgs<AssetPairInputContract, AssetPair>(
                settings, new {id = assetPairId});
        }

        private AssetPairContract Convert(IAssetPair settings)
        {
            return _convertService.Convert<IAssetPair, AssetPairContract>(settings);
        }
    }
}