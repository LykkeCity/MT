// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
