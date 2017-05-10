using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class MatchingEngineRouter : IMatchingEngineRouter
    {
        private readonly IMatchingEngineRoutesCacheService _routesCacheService;
        private readonly IMatchingEngineRepository _matchingEngineRepository;

        public MatchingEngineRouter(
            IMatchingEngineRoutesCacheService routesCacheService,
            IMatchingEngineRepository matchingEngineRepository)
        {
            _routesCacheService = routesCacheService;
            _matchingEngineRepository = matchingEngineRepository;
        }

        public object GetMatchingEngine(string clientId, string tradingConditionId, string instrument, OrderDirection orderType)
        {
            var route = _routesCacheService.GetMatchingEngineRoute(clientId, tradingConditionId, instrument, orderType);

            return _matchingEngineRepository.GetMatchingEngineById(route?.MatchingEngineId ?? MatchingEngines.Lykke);
        }
    }
}
