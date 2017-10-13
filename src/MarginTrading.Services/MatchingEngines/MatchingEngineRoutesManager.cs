using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Core;
using MarginTrading.Core.MatchingEngines;

namespace MarginTrading.Services.MatchingEngines
{
    public class MatchingEngineRoutesManager : IStartable
    {
        private const string AnyValue = "*";

        private readonly MatchingEngineRoutesCacheService _routesCacheService;
        private readonly IMatchingEngineRoutesRepository _repository;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly ITradingConditionsCacheService _tradingConditionsCacheService;
        private readonly IAccountsCacheService _accountsCacheService;

        public MatchingEngineRoutesManager(
            MatchingEngineRoutesCacheService routesCacheService,
            IMatchingEngineRoutesRepository repository,
            IAssetPairsCache assetPairsCache,
            ITradingConditionsCacheService tradingConditionsCacheService,
            IAccountsCacheService accountsCacheService)
        {
            _routesCacheService = routesCacheService;
            _repository = repository;            
            _assetPairsCache = assetPairsCache;
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

        public IMatchingEngineRoute GetRouteById(string id)
        {
            return ToViewModel(_routesCacheService.GetRoute(id));
        }

        public IMatchingEngineRoute FindRoute(string clientId, string tradingConditionId, string instrumentId, OrderDirection orderType)
        {
            var routes = _routesCacheService.GetRoutes();
            var instrument = string.IsNullOrEmpty(instrumentId)
                ? null
                : _assetPairsCache.GetAssetPairById(instrumentId);

            var topRankRoutes = routes
                .Where(r => EqualsOrAny(r.ClientId, clientId)
                            && EqualsOrAny(r.TradingConditionId, tradingConditionId)
                            && EqualsOrAny(r.Instrument, instrumentId)
                            && (!r.Type.HasValue || IsAny(r.Instrument) || r.Type.Value == orderType)
                            && IsAssetMatches(r.Asset, instrument, orderType))
                .GroupBy(r => r.Rank)
                .OrderBy(gr => gr.Key)
                .FirstOrDefault()?
                .Select(gr => gr)
                .ToArray();

            if (topRankRoutes == null)
                return null;

            if (topRankRoutes.Length == 1)
                return topRankRoutes[0];

            var mostSpecificRoutes = topRankRoutes
                .GroupBy(GetSpecificationLevel)
                .OrderByDescending(gr => gr.Key)
                .First()
                .Select(gr => gr)
                .ToArray();

            if (mostSpecificRoutes.Length == 1)
                return mostSpecificRoutes[0];

            var highestPriorityRoutes = mostSpecificRoutes
                .GroupBy(GetSpecificationPriority)
                .OrderByDescending(gr => gr.Key)
                .First()
                .Select(gr => gr)
                .ToArray();

            if (highestPriorityRoutes.Length == 1)
                return highestPriorityRoutes[0];

            return null;
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
            var matchingEngineRoute = MatchingEngineRoute.Create(route);

            matchingEngineRoute.ClientId = GetValueOrAnyIfValid(route.ClientId, ValidateClient);
            matchingEngineRoute.TradingConditionId = GetValueOrAnyIfValid(route.TradingConditionId, ValidateTradingCondition);
            matchingEngineRoute.Instrument = GetValueOrAnyIfValid(route.Instrument, ValidateInstrument);
            matchingEngineRoute.Asset = GetValueOrAnyIfValid(route.Asset, null);

            await _repository.AddOrReplaceRouteAsync(matchingEngineRoute);
            await UpdateRoutesCacheAsync();
        }        
                
        public async Task DeleteRouteAsync(string routeId)
        {
            await _repository.DeleteRouteAsync(routeId);
            await UpdateRoutesCacheAsync();
        }


        #region RouteValidation

        private string GetValueOrAnyIfValid(string value, Action<string> validationAction)
        {
            if (string.IsNullOrEmpty(value))
                return AnyValue;

            validationAction?.Invoke(value);

            return value;
        }

        private void ValidateClient(string clientId)
        {
            var userAccounts = _accountsCacheService.GetAll(clientId);
                
            if (!userAccounts.Any())
                throw new ArgumentException("Invalid ClientId");
        }

        private void ValidateTradingCondition(string tradingConditionId)
        {
            var tradingCondition = _tradingConditionsCacheService.GetTradingCondition(tradingConditionId);

            if (tradingCondition == null)
                throw new ArgumentException("Invalid TradingConditionId");
        }

        private void ValidateInstrument(string instrumentId)
        {
            var instrument = _assetPairsCache.GetAssetPairById(instrumentId);

            if (instrument == null)
                throw new ArgumentException("Invalid Instrument");
        }

        #endregion


        #region Helpers

        private static string GetEmptyIfAny(string value)
        {
            return value == AnyValue ? null : value;
        }

        private static IMatchingEngineRoute ToViewModel(IMatchingEngineRoute route)
        {
            return new MatchingEngineRoute
            {
                Id = route.Id,
                Rank = route.Rank,
                MatchingEngineId = route.MatchingEngineId,
                Asset = GetEmptyIfAny(route.Asset),
                ClientId = GetEmptyIfAny(route.ClientId),
                Instrument = GetEmptyIfAny(route.Instrument),
                TradingConditionId = GetEmptyIfAny(route.TradingConditionId),
                Type = route.Type
            };
        }

        private static bool IsAny(string value)
        {
            return value == AnyValue;
        }

        private static bool EqualsOrAny(string sourceValue, string targetValue)
        {
            return IsAny(sourceValue) || sourceValue == targetValue;
        }

        private static bool IsAssetMatches(string ruleAsset, IAssetPair instrument,
            OrderDirection? orderType)
        {
            if (IsAny(ruleAsset))
            {
                return true;
            }

            if (orderType == OrderDirection.Buy)
            {
                return instrument.QuoteAssetId == ruleAsset;
            }

            if (orderType == OrderDirection.Sell)
            {
                return instrument.BaseAssetId == ruleAsset;
            }

            return instrument.QuoteAssetId == ruleAsset || instrument.BaseAssetId == ruleAsset;
        }

        public static int GetSpecificationLevel(IMatchingEngineRoute route)
        {
            int specLevel = 0;
            specLevel += IsAny(route.Instrument) ? 0 : 1;
            specLevel += !route.Type.HasValue ? 0 : 1;
            specLevel += IsAny(route.TradingConditionId) ? 0 : 1;
            specLevel += IsAny(route.ClientId) ? 0 : 1;
            specLevel += IsAny(route.Asset) ? 0 : 1;
            return specLevel;
        }

        public static int GetSpecificationPriority(IMatchingEngineRoute route)
        {
            //matching field or wildcard in such priority:
            //client, trading condition, type, instrument, asset type, asset.
            //Flag based 1+2+4+8+16+32 >> 1 with Client(32) is higher than 1 with TradingConditionId(16), Type(8), Instrument(4), Asset Type(2), Asset(1) = 31

            int priority = 0;
            priority += IsAny(route.Asset) ? 0 : 1;
            priority += IsAny(route.Instrument) ? 0 : 4;
            priority += route.Type == null ? 0 : 8;
            priority += IsAny(route.TradingConditionId) ? 0 : 16;
            priority += IsAny(route.ClientId) ? 0 : 32;
            return priority;
        }

        #endregion

    }
}
