using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public class MatchingEngineRouter : IMatchingEngineRouter
    {
        private const string Lykke = "LYKKE";
        
        private readonly MatchingEngineRoutesManager _routesManager;
        private readonly IMatchingEngineRepository _matchingEngineRepository;

        public MatchingEngineRouter(
            MatchingEngineRoutesManager routesManager,
            IMatchingEngineRepository matchingEngineRepository)
        {
            _routesManager = routesManager;
            _matchingEngineRepository = matchingEngineRepository;
        }

        public IMatchingEngineBase GetMatchingEngineForOpen(IOrder order)
        {
            var route = _routesManager.FindRoute(order.ClientId, order.TradingConditionId, order.Instrument, order.GetOrderType());

            //TODO: use ME from trading condition
            return _matchingEngineRepository.GetMatchingEngineById(route?.MatchingEngineId ?? MatchingEngineConstants.LykkeVuMm);
        }

        public IMatchingEngineBase GetMatchingEngineForClose(IOrder order)
        {
            //TODO: use open ME or from trading condition + compare with Lykke const for old orders
            return _matchingEngineRepository.GetMatchingEngineById(MatchingEngineConstants.LykkeVuMm);
        }
    }
}
