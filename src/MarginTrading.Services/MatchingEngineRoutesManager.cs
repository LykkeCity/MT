using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Core;
using System.Linq;

namespace MarginTrading.Services
{
    public class MatchingEngineRoutesManager : IStartable
    {
        private const string AnyValue = "*";

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

        public IMatchingEngineRoute GetRouteById(string id)
        {
            return ToViewModel(_routesCacheService.GetRoute(id));
        }

        public IMatchingEngineRoute FindRoute(string clientId, string tradingConditionId, string instrumentId, OrderDirection orderType)
        {
            var routes = _routesCacheService.GetRoutes();
            var instrument = string.IsNullOrEmpty(instrumentId)
                ? null
                : _instrumentsCache.GetInstrumentById(instrumentId);

            var topRankRoutes = routes
                .Where(r => EqualsOrAny(r.ClientId, clientId)
                            && EqualsOrAny(r.TradingConditionId, tradingConditionId)
                            && EqualsOrAny(r.Instrument, instrumentId)
                            && (!r.Type.HasValue || r.Type.Value == orderType)
                            && (IsAny(r.Asset)
                                || (!r.AssetType.HasValue &&
                                    (r.Asset == instrument?.BaseAssetId ||
                                     r.Asset == instrument?.QuoteAssetId))
                                || (r.AssetType == AssetType.Base &&
                                    r.Asset == instrument?.BaseAssetId)
                                || (r.AssetType == AssetType.Quote &&
                                    r.Asset == instrument?.QuoteAssetId)))
                .GroupBy(r => r.Rank)
                .OrderByDescending(gr => gr.Key)
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
            var userAccount = _accountsCacheService.GetAll().FirstOrDefault(a => a.ClientId == clientId);
                
            if (userAccount == null)
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
            var instrument = _instrumentsCache.GetInstrumentById(instrumentId);

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
                AssetType = route.AssetType,
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

        public static int GetSpecificationLevel(IMatchingEngineRoute route)
        {
            int specLevel = 0;
            specLevel += IsAny(route.Instrument) ? 0 : 1;
            specLevel += !route.Type.HasValue ? 0 : 1;
            specLevel += IsAny(route.TradingConditionId) ? 0 : 1;
            specLevel += IsAny(route.ClientId) ? 0 : 1;
            specLevel += IsAny(route.Asset) ? 0 : 1;
            specLevel += !route.AssetType.HasValue ? 0 : 1;
            return specLevel;
        }

        public static int GetSpecificationPriority(IMatchingEngineRoute route)
        {
            //matching field or wildcard in such priority:
            //client, trading condition, type, instrument, asset type, asset.
            //Flag based 1+2+4+8+16+32 >> 1 with Client(32) is higher than 1 with TradingConditionId(16), Type(8), Instrument(4), Asset Type(2), Asset(1) = 31

            int priority = 0;
            priority += IsAny(route.Asset) ? 0 : 1;
            priority += route.AssetType == null ? 0 : 2;
            priority += IsAny(route.Instrument) ? 0 : 4;
            priority += route.Type == null ? 0 : 8;
            priority += IsAny(route.TradingConditionId) ? 0 : 16;
            priority += IsAny(route.ClientId) ? 0 : 32;
            return priority;
        }

        #endregion

    }
}
