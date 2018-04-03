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

        public IMatchingEngineBase GetMatchingEngineForOpen(IOrder order)
        {
            var route = _routesManager.FindRoute(order.ClientId, order.TradingConditionId, order.Instrument,
                order.GetOrderType());

            if (route != null)
            {
                //TODO: to consider account LE and take only ME with same LE as account
                
                return _matchingEngineRepository.GetMatchingEngineById(route.MatchingEngineId);
            }

            var assetPairSetting = _assetPairsCache.TryGetAssetPairById(order.Instrument);

            //TODO: find ME with correct mode that ownes the same Entity as asset pair
            return _matchingEngineRepository.GetMatchingEngineById(
                (assetPairSetting?.MatchingEngineMode ?? MatchingEngineMode.MarketMaker) == MatchingEngineMode.MarketMaker
                    ? MatchingEngineConstants.LykkeVuMm
                    : MatchingEngineConstants.LykkeCyStp);
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
