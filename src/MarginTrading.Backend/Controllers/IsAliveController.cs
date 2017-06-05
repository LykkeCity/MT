using MarginTrading.Core;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly IMatchingEngine _matchingEngine;
        private readonly ITradingEngine _tradingEngine;

        public IsAliveController(
            IMatchingEngine matchingEngine,
            ITradingEngine tradingEngine)
        {
            _matchingEngine = matchingEngine;
            _tradingEngine = tradingEngine;
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
                    Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion
            };
        }

        public class IsAliveResponse
        {
            public bool MatchingEngineAlive { get; set; }
            public bool TradingEngineAlive { get; set; }
            public string Version { get; set; }
        }
    }
}
