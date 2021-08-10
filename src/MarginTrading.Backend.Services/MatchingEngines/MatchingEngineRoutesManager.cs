// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.AzureRepositories.Logs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;
using MarginTrading.AssetService.Contracts;
using MarginTrading.AssetService.Contracts.Routes;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    [UsedImplicitly]
    public class MatchingEngineRoutesManager : IStartable, IMatchingEngineRoutesManager
    {
        private const string AnyValue = "*";

        private readonly MatchingEngineRoutesCacheService _routesCacheService;
        private readonly ITradingRoutesApi _routesApi;
        private readonly IAssetPairsCache _assetPairsCache;
        //private readonly IRiskSystemCommandsLogRepository _riskSystemCommandsLogRepository;
        private readonly ILog _log;
        private readonly IConvertService _convertService;

        public MatchingEngineRoutesManager(
            MatchingEngineRoutesCacheService routesCacheService,
            ITradingRoutesApi routesApi,
            IAssetPairsCache assetPairsCache,
            IAccountsCacheService accountsCacheService,
            //IRiskSystemCommandsLogRepository riskSystemCommandsLogRepository,
            ILog log,
            IConvertService convertService)
        {
            _routesCacheService = routesCacheService;
            _routesApi = routesApi;            
            _assetPairsCache = assetPairsCache;
            //_riskSystemCommandsLogRepository = riskSystemCommandsLogRepository;
            _log = log;
            _convertService = convertService;
        }

        
        #region Init
        
        public void Start()
        {
            UpdateRoutesCacheAsync().Wait();
        }
        
        public async Task UpdateRoutesCacheAsync()
        {
            var routes = await _routesApi.List();

            if (routes != null)
            {
                _routesCacheService.InitCache(
                    routes.Select(r => _convertService.Convert<MatchingEngineRouteContract, MatchingEngineRoute>(r)));
            }
            
            
        }
        
        #endregion

        
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
                            && IsAssetMatches(r.Asset, r.Type, instrument, orderType))
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

            if (highestPriorityRoutes.Length >= 1 &&
                highestPriorityRoutes.Select(r => r.MatchingEngineId).Distinct().Count() == 1)
            {
                return highestPriorityRoutes[0];
            }

            return null;
        }

       //TODO: Risk manager related stuff may be removed one time..
        
       public Task AddOrReplaceRouteInCacheAsync(IMatchingEngineRoute route)
        {            
            // Create Editable Object 
            var matchingEngineRoute = MatchingEngineRoute.Create(route);

            matchingEngineRoute.ClientId = GetValueOrAnyIfValid(route.ClientId, null);
            matchingEngineRoute.TradingConditionId = GetValueOrAnyIfValid(route.TradingConditionId, null);
            matchingEngineRoute.Instrument = GetValueOrAnyIfValid(route.Instrument, ValidateInstrument);
            matchingEngineRoute.Asset = GetValueOrAnyIfValid(route.Asset, null);

            _routesCacheService.SaveRoute(matchingEngineRoute);
            
            return Task.CompletedTask;
        }

        public Task DeleteRouteFromCacheAsync(string routeId)
        {
            _routesCacheService.DeleteRoute(routeId);
            
            return Task.CompletedTask;
        }

        public async Task HandleRiskManagerCommand(MatchingEngineRouteRisksCommand command)
        {
            try
            {
                switch (command.RequiredNotNull(nameof(command)).ActionType)
                {
                    case RiskManagerActionType.BlockTradingForNewOrders:
                        await HandleRiskManagerBlockTradingCommand(command);
                        break;
                    
                    case RiskManagerActionType.ExternalExchangePassThrough:
                        await HandleRiskManagerHedgingCommand(command);
                        break;
                    
                    default:
                        throw new NotSupportedException($"Command of type [{command.ActionType}] from risk manager is not supported");
                }

                //await _riskSystemCommandsLogRepository.AddProcessedAsync(command.ActionType.ToString(),
                //    command.ToJson());
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(HandleRiskManagerCommand), command, e);
                //await _riskSystemCommandsLogRepository.AddErrorAsync(command.ActionType.ToString(), command.ToJson(),
                //    e.Message);
                throw;
            }
        }

        public async Task HandleRiskManagerBlockTradingCommand(MatchingEngineRouteRisksCommand command)
        {
            switch (command.Action)
            {
                case RiskManagerAction.On:

                    var routes = FindRejectRoutes(command);

                    if (routes.Any())
                    {
                        //await _riskSystemCommandsLogRepository.AddErrorAsync(command.ActionType.ToString(),
                        //    command.ToJson(),
                        //    $"Route already exists: {routes.ToJson()}");
                        await _log.WriteWarningAsync(nameof(MatchingEngineRoutesManager),
                            nameof(HandleRiskManagerBlockTradingCommand), routes.ToJson(),
                            $"Route already exists. Command from risk system is not processed: {command.ToJson()} ");
                        return;
                    }
                    
                    var newRoute = new MatchingEngineRoute
                    {
                        Id = Guid.NewGuid().ToString().ToUpper(),
                        Rank = command.Rank,
                        Asset = command.Asset,
                        ClientId = command.ClientId,
                        Instrument = command.Instrument,
                        TradingConditionId = command.TradingConditionId,
                        Type = GetOrderDirection(command.Direction),
                        MatchingEngineId = MatchingEngineConstants.Reject,
                        RiskSystemLimitType = command.LimitType,
                        RiskSystemMetricType = command.Type
                    };

                    await AddOrReplaceRouteInCacheAsync(newRoute);
                    break;
                    
                case RiskManagerAction.Off:

                    var existingRoutes = FindRejectRoutes(command);

                    if (existingRoutes.Length != 1)
                        throw new Exception(
                            $"Cannot disable BlockTradingForNewOrders route for command: {command.ToJson()}. Existing routes found for command: {existingRoutes.ToJson()}");

                    await DeleteRouteFromCacheAsync(existingRoutes[0].Id);
                    break;
                    
                default:
                    throw new NotSupportedException($"Action [{command.Action}] from risk managment system is not supported.");
                    
            }
        }

        private IMatchingEngineRoute[] FindRejectRoutes(MatchingEngineRouteRisksCommand command)
        {
            var allRoutes = _routesCacheService.GetRoutes();

            return allRoutes.Where(r => r.Rank == command.Rank &&
                                        r.Asset == GetValueOrAny(command.Asset) &&
                                        r.ClientId == GetValueOrAny(command.ClientId) &&
                                        r.Instrument == GetValueOrAny(command.Instrument) &&
                                        r.TradingConditionId ==
                                        GetValueOrAny(command.TradingConditionId) &&
                                        r.Type == GetOrderDirection(command.Direction) &&
                                        r.MatchingEngineId == MatchingEngineConstants.Reject &&
                                        r.RiskSystemLimitType == command.LimitType &&
                                        r.RiskSystemMetricType == command.Type).ToArray();
        }

        private Task HandleRiskManagerHedgingCommand(MatchingEngineRouteRisksCommand command)
        {
            //TODO: change when 1 to 1 hedging will be implemented
            return HandleRiskManagerBlockTradingCommand(command);
        }
        
        
        #region RouteValidation

        private string GetValueOrAny(string value)
        {
            return string.IsNullOrEmpty(value) ? AnyValue : value;
        }
        
        private string GetValueOrAnyIfValid(string value, Action<string> validationAction)
        {
            if (string.IsNullOrEmpty(value))
                return AnyValue;

            validationAction?.Invoke(value);

            return value;
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

        private static bool IsAssetMatches(string ruleAsset, OrderDirection? ruleType, IAssetPair instrument,
            OrderDirection orderType)
        {
            if (IsAny(ruleAsset))
            {
                return true;
            }

            switch (ruleType)
            {
                case OrderDirection.Buy:
                    return (orderType == OrderDirection.Buy && instrument.BaseAssetId == ruleAsset) ||
                           (orderType == OrderDirection.Sell && instrument.QuoteAssetId == ruleAsset);
                    
                case OrderDirection.Sell:
                    return (orderType == OrderDirection.Sell && instrument.BaseAssetId == ruleAsset) ||
                           (orderType == OrderDirection.Buy && instrument.QuoteAssetId == ruleAsset);
                    
                case null:
                    return instrument.QuoteAssetId == ruleAsset || instrument.BaseAssetId == ruleAsset;
            }

            return false;
        }

        public static int GetSpecificationLevel(IMatchingEngineRoute route)
        {
            var specLevel = 0;
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

            var priority = 0;
            priority += IsAny(route.Asset) ? 0 : 1;
            priority += IsAny(route.Instrument) ? 0 : 4;
            priority += route.Type == null ? 0 : 8;
            priority += IsAny(route.TradingConditionId) ? 0 : 16;
            priority += IsAny(route.ClientId) ? 0 : 32;
            return priority;
        }

        private OrderDirection? GetOrderDirection(RouteDirection? routeDirection)
        {
            switch (routeDirection)
            {
                case null:
                    return null;
                
                case RouteDirection.Buy:
                    return OrderDirection.Buy;
                    
                case RouteDirection.Sell:
                    return OrderDirection.Sell;
                    
                default:
                    throw new NotSupportedException($"Route direction [{routeDirection}] is not supported");
            }
        }

        #endregion

    }
}
