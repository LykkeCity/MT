using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Services;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly MarginTradingSettings _settings;
        private readonly IDateService _dateService;

        public IsAliveController(
            MarginTradingSettings settings,
            IDateService dateService)
        {
            _settings = settings;
            _dateService = dateService;
        }
        
        [HttpGet]
        public IsAliveResponse Get()
        {
            return new IsAliveResponse
            {
                Version =
                    Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion,
                Env = _settings.Env,
                ServerTime = _dateService.Now()
            };
        }
    }
}
