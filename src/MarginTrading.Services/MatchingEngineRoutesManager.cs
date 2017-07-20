using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Core;
using System.Linq;

namespace MarginTrading.Services
{
    public class MatchingEngineRoutesManager : IStartable
    {
        private readonly MatchingEngineRoutesCacheService _routesCacheService;
        private readonly IMatchingEngineRoutesRepository _repository;
        private readonly IInstrumentsCache _instrumentsCache;
        private readonly ITradingConditionsCacheService _tradingConditionsCacheService;
        private readonly IAccountsCacheService _accountsCacheService;

        public MatchingEngineRoutesManager(
            MatchingEngineRoutesCacheService routesCacheService,
            IMatchingEngineRoutesRepository repository,
            IInstrumentsCache instrumentsCache,
            ITradingConditionsCacheService tradingConditionsCacheService,
            IAccountsCacheService accountsCacheService)
        {
            _routesCacheService = routesCacheService;
            _repository = repository;            
            _instrumentsCache = instrumentsCache;
            _tradingConditionsCacheService = tradingConditionsCacheService;
            _accountsCacheService = accountsCacheService;
        }

        public void Start()
        {
            UpdateRoutesCacheAsync().Wait();
        }

        public IEnumerable<IMatchingEngineRoute> GetRoutes()
        {
            return _routesCacheService.GetRoutes().Select(ToViewModel);
        }
        public IMatchingEngineRoute GetRoute(string id)
        {
            return ToViewModel(_routesCacheService.GetRoute(id));
        }
        private static IMatchingEngineRoute ToViewModel(IMatchingEngineRoute route)
        {
            return new MatchingEngineRoute()
            {
                Id = route.Id,
                Rank = route.Rank,
                MatchingEngineId = route.MatchingEngineId,
                Asset = route.Asset,
                AssetType = route.AssetType,
                ClientId = route.ClientId == "*" ? null : route.ClientId,
                Instrument = route.Instrument == "*" ? null : route.Instrument,
                TradingConditionId = route.TradingConditionId == "*" ? null : route.TradingConditionId,
                Type = route.Type
            };
        }

        public async Task UpdateRoutesCacheAsync()
        {
            var routes = new List<IMatchingEngineRoute>();
            routes.AddRange(await _repository.GetAllRoutesAsync());
            _routesCacheService.InitCache(routes);
        }

        public async Task AddOrReplaceRouteAsync(IMatchingEngineRoute route)
        {            
            // Create Editable Object 
            MatchingEngineRoute matchingEngineRoute = new MatchingEngineRoute()
            {
                Id = route.Id,
                Instrument = route.Instrument,
                ClientId = route.ClientId,
                TradingConditionId = route.TradingConditionId,
                Rank = route.Rank,
                Type = route.Type,
                MatchingEngineId = route.MatchingEngineId,
                Asset = route.Asset,
                AssetType = route.AssetType
            };

            // Validate Client
            if (string.IsNullOrEmpty(route.ClientId))
                matchingEngineRoute.ClientId = "*"; // Wildcard
            else
            {
                var userAccount = (from tc in _accountsCacheService.GetAll()
                                   where tc.ClientId == route.ClientId
                                   select tc).FirstOrDefault();
                if (userAccount == null)
                    throw new System.ArgumentException("Invalid ClientId");
                else
                    matchingEngineRoute.ClientId = userAccount.ClientId;

            }

            // Validate TradingCondition
            if (string.IsNullOrEmpty(route.TradingConditionId))
                matchingEngineRoute.TradingConditionId = "*"; // Wildcard
            else
            {
                var tradingCondition = _tradingConditionsCacheService.GetTradingCondition(route.TradingConditionId);
                if (tradingCondition == null)
                    throw new System.ArgumentException("Invalid TradingConditionId");
                else
                    matchingEngineRoute.TradingConditionId = tradingCondition.Id;
            }

            // Validate Instrument
            if (string.IsNullOrEmpty(route.Instrument))
                matchingEngineRoute.Instrument = "*"; // Wildcard
            else
            {
                var instrument = _instrumentsCache.GetInstrumentById(route.Instrument);
                if (instrument == null)
                    throw new System.ArgumentException("Invalid Instrument");
                else
                    matchingEngineRoute.Instrument = instrument.Id;
            }

            await _repository.AddOrReplaceRouteAsync(matchingEngineRoute);
            await UpdateRoutesCacheAsync();
        }        
                
        public async Task DeleteRouteAsync(string routeId)
        {
            await _repository.DeleteRouteAsync(routeId);
            await UpdateRoutesCacheAsync();
        }
    }
}
