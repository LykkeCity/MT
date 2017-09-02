using MarginTrading.Core.Settings;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
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
        public IActionResult GetIsAlive()
        {
            return Ok(new
            {
                Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion,
                Env = _settings.IsLive ? "Live" : "Demo"
            });
        }
    }
}
