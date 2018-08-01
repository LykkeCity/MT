using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.AssetSettings;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Route("api/[controller]"), Authorize]
    public class AssetController : Controller, IAssetReadingApi
    {
        private readonly IConvertService _convertService;
        private readonly IAssetRepository _assetRepository;

        public AssetController(IConvertService convertService,
            IAssetRepository assetRepository)
        {
            _convertService = convertService;
            _assetRepository = assetRepository;
        }

        /// <summary>
        /// Get assets 
        /// </summary>
        [HttpGet, Route("")]
        public async Task<List<AssetContract>> List()
        {
            return (await _assetRepository.GetAsync()).Select(Convert).ToList();
        }

        /// <summary>
        /// Get asset by id
        /// </summary>
        [HttpGet, Route("{assetId}")]
        public async Task<AssetContract> Get(string assetPairId)
        {
            return Convert(await _assetRepository.GetAsync(assetPairId));
        }

        private AssetContract Convert(IAsset settings)
        {
            return _convertService.Convert<IAsset, AssetContract>(settings);
        }
    }
}