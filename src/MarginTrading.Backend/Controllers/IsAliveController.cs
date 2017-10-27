using MarginTrading.Common.BackendContracts;
using MarginTrading.Core;
using MarginTrading.Core.MatchingEngines;
using MarginTrading.Core.Settings;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly IInternalMatchingEngine _matchingEngine;
        private readonly ITradingEngine _tradingEngine;
        private readonly MarginSettings _settings;
        private readonly IDateService _dateService;

        public IsAliveController(
            IInternalMatchingEngine matchingEngine,
            ITradingEngine tradingEngine,
            MarginSettings settings,
            IDateService dateService)
        {
            _matchingEngine = matchingEngine;
            _tradingEngine = tradingEngine;
            _settings = settings;
            _dateService = dateService;
        }
        [HttpGet]
        public IsAliveResponse Get()
        {
            bool matchingEngineAlive = _matchingEngine.PingLock();
            bool tradingEngineAlive = _tradingEngine.PingLock();

            return new IsAliveResponse
            {
                MatchingEngineAlive = matchingEngineAlive,
                TradingEngineAlive = tradingEngineAlive,
                Version =
                    Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion,
                Env = _settings.IsLive ? "Live" : "Demo",
                ServerTime = _dateService.Now()
            };
        }
    }
}
