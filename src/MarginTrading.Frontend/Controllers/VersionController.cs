using MarginTrading.Frontend.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.PlatformAbstractions;

namespace MarginTrading.Frontend.Controllers
{
    [Route("home/[controller]")]
    [Route("api/isAlive")]
    public class VersionController : Controller
    {
        private readonly MtFrontendSettings _setings;

        public VersionController(MtFrontendSettings setings)
        {
            _setings = setings;
        }

        [HttpGet]
        public VersionModel Get()
        {
            return new VersionModel
            {
                Version = PlatformServices.Default.Application.ApplicationVersion,
                Env = _setings.MarginTradingFront.Env
            };
        }

        public class VersionModel
        {
            public string Version { get; set; }
            public string Env { get; set; }
        }
    }
}
