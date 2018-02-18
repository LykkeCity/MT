using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public class MatchingEngineRouter : IMatchingEngineRouter
    {
        private const string Lykke = "LYKKE";
        
        private readonly MatchingEngineRoutesManager _routesManager;
        private readonly IMatchingEngineRepository _matchingEngineRepository;
        private readonly ITradingConditionsCacheService _tradingConditionsCacheService;

        public MatchingEngineRouter(
            MatchingEngineRoutesManager routesManager,
            IMatchingEngineRepository matchingEngineRepository,
            ITradingConditionsCacheService tradingConditionsCacheService)
        {
            _routesManager = routesManager;
            _matchingEngineRepository = matchingEngineRepository;
            _tradingConditionsCacheService = tradingConditionsCacheService;
        }

        public IMatchingEngineBase GetMatchingEngineForOpen(IOrder order)
        {
            var route = _routesManager.FindRoute(order.ClientId, order.TradingConditionId, order.Instrument,
                order.GetOrderType());

            if (route != null)
            {
                return _matchingEngineRepository.GetMatchingEngineById(route.MatchingEngineId);
            }

            var tradingCondition = _tradingConditionsCacheService.GetTradingCondition(order.TradingConditionId);

            return _matchingEngineRepository.GetMatchingEngineById(
                tradingCondition.MatchingEngineId ?? MatchingEngineConstants.LykkeVuMm);
        }

        public IMatchingEngineBase GetMatchingEngineForClose(IOrder order)
        {
            var meId = order.OpenOrderbookId == Lykke
                ? MatchingEngineConstants.LykkeVuMm
                : order.OpenOrderbookId;
            
            return _matchingEngineRepository.GetMatchingEngineById(meId);
        }
    }
}
