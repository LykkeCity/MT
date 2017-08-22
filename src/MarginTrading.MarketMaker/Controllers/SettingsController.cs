using System;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Models;
using MarginTrading.MarketMaker.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;

namespace MarginTrading.MarketMaker.Controllers
{
    [Route("api/[controller]")]
    public class SettingsController : Controller
    {
        private readonly IMarketMakerService _marketMakerService;
        private readonly IAssetPairsSettingsService _assetPairsSettingsService;

        public SettingsController(IMarketMakerService marketMakerService, IAssetPairsSettingsService assetPairsSettingsService)
        {
            _marketMakerService = marketMakerService;
            _assetPairsSettingsService = assetPairsSettingsService;
        }

        [HttpPost]
        [SwaggerOperation("SetSettings")]
        public async Task<IActionResult> Post([FromBody] AssetPairSettingsModel settings)
        {
            await _marketMakerService.ProcessAssetPairSettingsAsync(settings);
            return Ok(new {success = true});
        }

        [HttpGet]
        [SwaggerOperation("GetSettingsList")]
        public async Task<IActionResult> GetCurrentSettingsList()
        {
            var result = await _assetPairsSettingsService.GetAllPairsSources();
            return Ok(result);
        }
    }
}