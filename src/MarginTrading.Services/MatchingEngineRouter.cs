using MarginTrading.Core;

namespace MarginTrading.Services
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

        public object GetMatchingEngine(string clientId, string tradingConditionId, string instrument, OrderDirection orderType)
        {
            var route = _routesManager.FindRoute(clientId, tradingConditionId, instrument, orderType);

            return _matchingEngineRepository.GetMatchingEngineById(route?.MatchingEngineId ?? MatchingEngines.Lykke);
        }
    }
}
