using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Models.Api;
using MarginTrading.MarketMaker.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;

namespace MarginTrading.MarketMaker.Controllers
{
    [Route("api/[controller]")]
    public class ExtPriceSettingsController : Controller
    {
        private readonly IPriceCalcSettingsService _priceCalcSettingsService;

        public ExtPriceSettingsController(
            IPriceCalcSettingsService priceCalcSettingsService)
        {
            _priceCalcSettingsService = priceCalcSettingsService;
        }

        /// <summary>
        /// Inserts or updates settings for an asset pair
        /// </summary>
        [HttpPost]
        [Route("set")]
        [SwaggerOperation("SetExtPriceSettings")]
        public async Task<IActionResult> Set([FromBody] IEnumerable<AssetPairExtPriceSettingsModel> settings)
        {
            await Task.WhenAll(settings.Select(s => _priceCalcSettingsService.Set(s)));
            return Ok(new {success = true});
        }

        /// <summary>
        /// Gets all existing settings
        /// </summary>
        [HttpGet]
        [Route("")]
        [SwaggerOperation("GetAllExtPriceSettings")]
        public Task<IReadOnlyList<AssetPairExtPriceSettingsModel>> GetAll()
        {
            return _priceCalcSettingsService.GetAllAsync();
        }

        /// <summary>
        /// Set settings for a single asset pair
        /// </summary>
        [HttpGet]
        [Route("{assetPairId}")]
        [SwaggerOperation("GetExtPriceSettings")]
        [CanBeNull]
        public Task<IReadOnlyList<AssetPairExtPriceSettingsModel>> Get(string assetPairId)
        {
            return _priceCalcSettingsService.GetAllAsync(assetPairId);
        }

        /// <summary>
        /// Gets all hedging preferences
        /// </summary>
        [HttpGet]
        [Route("hedging-preferences")]
        [SwaggerOperation("GetAllExtHedgingPreferences")]
        public IReadOnlyList<HedgingPreferenceModel> GetAllHedgingPreferences()
        {
            return _priceCalcSettingsService.GetAllHedgingPreferences();
        }
    }
}