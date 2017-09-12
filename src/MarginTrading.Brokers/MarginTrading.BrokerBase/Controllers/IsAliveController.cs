using MarginTrading.BrokerBase.Settings;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.BrokerBase.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly CurrentApplicationInfo _applicationInfo;

        public IsAliveController(CurrentApplicationInfo applicationInfo)
        {
            _applicationInfo = applicationInfo;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new {
                ApplicationName = _applicationInfo.ApplicationName,
                Version = _applicationInfo.ApplicationVersion,
                Env = _applicationInfo.IsLive ? "Live" : "Demo",
            });
        }
    }
}
