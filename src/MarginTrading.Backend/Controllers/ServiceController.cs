using MarginTrading.Backend.Core;
using MarginTrading.Common.Middleware;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/service")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class ServiceController : Controller
    {
        private readonly IQuoteCacheService _quoteCacheService;

        public ServiceController(IQuoteCacheService quoteCacheService)
        {
            _quoteCacheService = quoteCacheService;
        }

        [HttpDelete]
        [Route("bestprice/{assetPair}")]
        public MtBackendResponse<bool> ClearBestBriceCache(string assetPair)
        {
            _quoteCacheService.RemoveQuote(assetPair);
            
            return MtBackendResponse<bool>.Ok(true);
        }
    }
}