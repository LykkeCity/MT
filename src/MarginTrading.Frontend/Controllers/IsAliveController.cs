using System.Threading.Tasks;
using MarginTrading.Common.Services;
using MarginTrading.Contract.ClientContracts;
using MarginTrading.Frontend.Services;
using MarginTrading.Frontend.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.PlatformAbstractions;

namespace MarginTrading.Frontend.Controllers
{
    [Route("api/isAlive")]
    public class IsAliveController : Controller
    {
        private readonly MtFrontendSettings _setings;
        private readonly IHttpRequestService _httpRequestService;
        private readonly WampSessionsService _wampSessionsService;
        private readonly IDateService _dateService;

        public IsAliveController(MtFrontendSettings setings,
            IHttpRequestService httpRequestService,
            WampSessionsService wampSessionsService,
            IDateService dateService)
        {
            _setings = setings;
            _httpRequestService = httpRequestService;
            _wampSessionsService = wampSessionsService;
            _dateService = dateService;
        }

        [HttpGet]
        public async Task<IsAliveExtendedResponse> Get()
        {
            var result = new IsAliveExtendedResponse
            {
                Version = PlatformServices.Default.Application.ApplicationVersion,
                Env = _setings.MarginTradingFront.Env,
                ServerTime = _dateService.Now()
            };

            result.LiveVersion = await GetBackendVersion(true);
            result.DemoVersion = await GetBackendVersion(false);
            result.WampOpened = _wampSessionsService.OpenedSessionsCount;

            return result;
        }

        private async Task<string> GetBackendVersion(bool isLive)
        {
            try
            {
                var responce = await _httpRequestService.GetAsync<IsAliveResponse>("isAlive", isLive, 3);
                return responce.Version;
            }
            catch (MaintenanceException ex)
            {
                return $"Maintenance since {ex.EnabledAt}";
            }
            catch
            {
                return "Error";
            }
            
        }
    }
}
