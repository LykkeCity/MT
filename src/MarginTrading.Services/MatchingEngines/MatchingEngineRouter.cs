using MarginTrading.Core;
using MarginTrading.Core.MatchingEngines;

namespace MarginTrading.Services.MatchingEngines
{
    public class MatchingEngineRouter : IMatchingEngineRouter
    {
        private readonly MatchingEngineRoutesManager _routesManager;
        private readonly IMatchingEngineRepository _matchingEngineRepository;

        public MatchingEngineRouter(
            MatchingEngineRoutesManager routesManager,
            IMatchingEngineRepository matchingEngineRepository)
        {
            _routesManager = routesManager;
            _matchingEngineRepository = matchingEngineRepository;
        }

        public IMatchingEngineBase GetMatchingEngine(string clientId, string tradingConditionId, string instrument, OrderDirection orderType)
        {
            var route = _routesManager.FindRoute(clientId, tradingConditionId, instrument, orderType);

            return _matchingEngineRepository.GetMatchingEngineById(route?.MatchingEngineId ?? MatchingEngineConstants.Lykke);
        }
    }
}
