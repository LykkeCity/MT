using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;

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

        //TODO: implement routes logic
        public IMatchingEngineBase GetMatchingEngineForOpen(IOrder order)
        {
            
            
            var route = _routesManager.FindRoute(null, order.TradingConditionId, order.Instrument,
                order.GetOrderDirection());

            if (route != null)
            {
                //TODO: to consider account LE and take only ME with same LE as account
                
                return _matchingEngineRepository.GetMatchingEngineById(route.MatchingEngineId);
            }

            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(order.Instrument);

            //TODO: find ME with correct mode that ownes the same Entity as asset pair
            return _matchingEngineRepository.GetMatchingEngineById(
                (assetPair?.MatchingEngineMode ?? MatchingEngineMode.MarketMaker) == MatchingEngineMode.MarketMaker
                    ? MatchingEngineConstants.Reject
                    : MatchingEngineConstants.DefaultStp);
        }

        public IMatchingEngineBase GetMatchingEngineForClose(IOrder order)
        {
            var meId = order.OpenOrderbookId == Lykke
                ? MatchingEngineConstants.Reject
                : order.OpenOrderbookId;
            
            return _matchingEngineRepository.GetMatchingEngineById(meId);
        }
    }
}
