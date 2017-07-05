using System.Threading.Tasks;
using MarginTrading.Common.ClientContracts;
using MarginTrading.Frontend.Services;
using MarginTrading.Frontend.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.PlatformAbstractions;
using IsAliveResponse = MarginTrading.Common.BackendContracts.IsAliveResponse;

namespace MarginTrading.Frontend.Controllers
{
    [Route("api/isAlive")]
    public class VersionController : Controller
    {
        private readonly MtFrontendSettings _setings;
        private readonly IHttpRequestService _httpRequestService;

        public VersionController(MtFrontendSettings setings,
            IHttpRequestService httpRequestService)
        {
            _setings = setings;
            _httpRequestService = httpRequestService;
        }

        [HttpGet]
        public async Task<IsAliveExtendedResponse> Get()
        {
            var result = new IsAliveExtendedResponse
            {
                Version = PlatformServices.Default.Application.ApplicationVersion,
                Env = _setings.MarginTradingFront.Env
            };

            try
            {
                var responce = await _httpRequestService.GetAsync<IsAliveResponse>("isAlive");
                result.LiveVersion = responce.Version;
            }
            catch
            {
                result.LiveVersion = "Error";
            }

            try
            {
                var responce = await _httpRequestService.GetAsync<IsAliveResponse>("isAlive", false);
                result.DemoVersion = responce.Version;
            }
            catch
            {
                result.DemoVersion = "Error";
            }

            return result;
        }
    }
}
