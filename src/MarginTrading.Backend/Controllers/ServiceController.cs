using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Infrastructure;
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
        private readonly IMaintenanceModeService _maintenanceModeService;
        private readonly IQuoteCacheService _quoteCacheService;

        public ServiceController(IMaintenanceModeService maintenanceModeService,
            IQuoteCacheService quoteCacheService)
        {
            _maintenanceModeService = maintenanceModeService;
            _quoteCacheService = quoteCacheService;
        }

        [HttpPost]
        [Route(LykkeConstants.MaintenanceModeRoute)]
        public MtBackendResponse<bool> SetMaintenanceMode([FromBody]bool enabled)
        {
            _maintenanceModeService.SetMode(enabled);

            return MtBackendResponse<bool>.Ok(enabled);
        }

        [HttpGet]
        [Route(LykkeConstants.MaintenanceModeRoute)]
        public MtBackendResponse<bool> GetMaintenanceMode()
        {
            var result = _maintenanceModeService.CheckIsEnabled();

            return MtBackendResponse<bool>.Ok(result);
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