using System.Threading.Tasks;
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

        public SettingsController(IMarketMakerService marketMakerService)
        {
            _marketMakerService = marketMakerService;
        }

        [HttpPost]
        [SwaggerOperation("SetSettings")]
        public async Task<IActionResult> Post([FromBody] AssetPairSettingsModel message)
        {
            await _marketMakerService.ProcessAssetPairSettingsAsync(message);
            return Ok(new { success = true });
        }
    }
}
