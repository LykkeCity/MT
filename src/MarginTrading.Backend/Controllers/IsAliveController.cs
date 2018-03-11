using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Services;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly IMarketMakerMatchingEngine _matchingEngine;
        private readonly ITradingEngine _tradingEngine;
        private readonly MarginSettings _settings;
        private readonly IDateService _dateService;

        public IsAliveController(
            IMarketMakerMatchingEngine matchingEngine,
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
            var matchingEngineAlive = _matchingEngine.PingLock();
            var tradingEngineAlive = _tradingEngine.PingLock();

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
