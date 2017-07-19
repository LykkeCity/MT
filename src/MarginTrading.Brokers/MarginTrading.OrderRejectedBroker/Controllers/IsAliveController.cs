using MarginTrading.Common.ClientContracts;
using MarginTrading.Core.Settings;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.OrderRejectedBroker.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly MarginSettings _settings;

        public IsAliveController(MarginSettings settings)
        {
            _settings = settings;
        }

        [HttpGet]
        public IsAliveResponse Get()
        {
            return new IsAliveResponse
            {
                Version =
                    Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion,
                Env = _settings.IsLive ? "Live" : "Demo"
            };
        }
    }
}
