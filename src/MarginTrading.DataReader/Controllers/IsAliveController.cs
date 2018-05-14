using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly Settings.DataReaderSettings _settings;

        public IsAliveController(Settings.DataReaderSettings settings)
        {
            _settings = settings;
        }

        [HttpGet]
        public IActionResult GetIsAlive()
        {
            return Ok(new
            {
                Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion,
                Env = Program.EnvInfo
            });
        }
    }
}
