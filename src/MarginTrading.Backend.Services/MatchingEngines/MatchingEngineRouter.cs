using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public class MatchingEngineRouter : IMatchingEngineRouter
    {
        private const string Lykke = "LYKKE";
        
        private readonly MatchingEngineRoutesManager _routesManager;
        private readonly IMatchingEngineRepository _matchingEngineRepository;
        private readonly IAssetPairsCache _assetPairsCache;

        public MatchingEngineRouter(
            MatchingEngineRoutesManager routesManager,
            IMatchingEngineRepository matchingEngineRepository,
            IAssetPairsCache assetPairsCache)
        {
            _routesManager = routesManager;
            _matchingEngineRepository = matchingEngineRepository;
            _assetPairsCache = assetPairsCache;
        }

        //TODO: implement routes logic, to consider account LE and take only ME with same LE as account, find ME with correct mode that owns the same Entity as asset pair
        public IMatchingEngineBase GetMatchingEngineForExecution(Order order)
        {
            var route = _routesManager.FindRoute(null, order.TradingConditionId, order.AssetPairId,
                order.Direction);

            if (route != null)
            {                
                return _matchingEngineRepository.GetMatchingEngineById(route.MatchingEngineId);
            }

            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(order.AssetPairId);

            return _matchingEngineRepository.GetMatchingEngineById(
                (assetPair?.MatchingEngineMode ?? MatchingEngineMode.MarketMaker) == MatchingEngineMode.MarketMaker
                    ? MatchingEngineConstants.DefaultMm
                    : MatchingEngineConstants.DefaultStp);
        }

        public IMatchingEngineBase GetMatchingEngineForClose(string openMatchingEngineId)
        {
            return _matchingEngineRepository.GetMatchingEngineById(openMatchingEngineId);
        }
    }
}
