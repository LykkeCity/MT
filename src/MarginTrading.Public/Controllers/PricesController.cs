using System.Linq;
using MarginTrading.Common.Mappers;
using MarginTrading.Public.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Public.Controllers
{
    [Route("api/[controller]")]
    public class PricesController : Controller
    {
        private readonly IPricesCacheService _pricesCacheService;

        public PricesController(IPricesCacheService pricesCacheService)
        {
            _pricesCacheService = pricesCacheService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var prices = _pricesCacheService.GetPrices();
            var clientPrices = prices.Select(item => item.ToClientContract()).ToArray();
            return Ok(clientPrices);
        }
    }
}
