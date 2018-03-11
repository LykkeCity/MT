﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using MarginTrading.AzureRepositories.Logs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public class MatchingEngineRoutesManager : IStartable
    {
        private const string AnyValue = "*";

        private readonly MatchingEngineRoutesCacheService _routesCacheService;
        private readonly IMatchingEngineRoutesRepository _repository;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly ITradingConditionsCacheService _tradingConditionsCacheService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IRiskSystemCommandsLogRepository _riskSystemCommandsLogRepository;
        private readonly ILog _log;

        public MatchingEngineRoutesManager(
            MatchingEngineRoutesCacheService routesCacheService,
            IMatchingEngineRoutesRepository repository,
            IAssetPairsCache assetPairsCache,
            ITradingConditionsCacheService tradingConditionsCacheService,
            IAccountsCacheService accountsCacheService,
            IRiskSystemCommandsLogRepository riskSystemCommandsLogRepository,
            ILog log)
        {
            _routesCacheService = routesCacheService;
            _repository = repository;            
            _assetPairsCache = assetPairsCache;
            _tradingConditionsCacheService = tradingConditionsCacheService;
            _accountsCacheService = accountsCacheService;
            _riskSystemCommandsLogRepository = riskSystemCommandsLogRepository;
            _log = log;
        }

        
        #region Init
        
        public void Start()
        {
            UpdateRoutesCacheAsync().Wait();
        }
        
        public async Task UpdateRoutesCacheAsync()
        {
            var routes = await _repository.GetAllRoutesAsync();
            
            _routesCacheService.InitCache(routes);
        }
        
        #endregion

        
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

       public async Task AddOrReplaceRouteAsync(IMatchingEngineRoute route)
        {            
            // Create Editable Object 
            var matchingEngineRoute = MatchingEngineRoute.Create(route);

            matchingEngineRoute.ClientId = GetValueOrAnyIfValid(route.ClientId, ValidateClient);
            matchingEngineRoute.TradingConditionId = GetValueOrAnyIfValid(route.TradingConditionId, ValidateTradingCondition);
            matchingEngineRoute.Instrument = GetValueOrAnyIfValid(route.Instrument, ValidateInstrument);
            matchingEngineRoute.Asset = GetValueOrAnyIfValid(route.Asset, null);

            await _repository.AddOrReplaceRouteAsync(matchingEngineRoute);
            _routesCacheService.SaveRoute(matchingEngineRoute);
        }        
                
        public async Task DeleteRouteAsync(string routeId)
        {
            await _repository.DeleteRouteAsync(routeId);
            _routesCacheService.DeleteRoute(routeId);
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

                await _riskSystemCommandsLogRepository.AddProcessedAsync(command.ActionType.ToString(),
                    command.ToJson());
            }
            catch (Exception e)
            {
                await _riskSystemCommandsLogRepository.AddErrorAsync(command.ActionType.ToString(), command.ToJson(),
                    e.Message);
                throw;
            }
        }

        private async Task HandleRiskManagerBlockTradingCommand(MatchingEngineRouteRisksCommand command)
        {
            switch (command.Action)
            {
                case RiskManagerAction.On:

                    var routes = FindRejectRoutes(command);

                    if (routes.Any())
                    {
                        await _riskSystemCommandsLogRepository.AddErrorAsync(command.ActionType.ToString(),
                            command.ToJson(),
                            $"Route already exists: {routes.ToJson()}");
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

                    await AddOrReplaceRouteAsync(newRoute);
                    break;
                    
                case RiskManagerAction.Off:

                    var existingRoutes = FindRejectRoutes(command);

                    if (existingRoutes.Length != 1)
                        throw new Exception(
                            $"Cannot disable BlockTradingForNewOrders route for command: {command.ToJson()}. Existing routes found for command: {existingRoutes.ToJson()}");

                    await DeleteRouteAsync(existingRoutes[0].Id);
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

        private void ValidateClient(string clientId)
        {
            var userAccounts = _accountsCacheService.GetAll(clientId);
                
            if (!userAccounts.Any())
                throw new ArgumentException("Invalid ClientId");
        }

        private void ValidateTradingCondition(string tradingConditionId)
        {
            if (!_tradingConditionsCacheService.IsTradingConditionExists(tradingConditionId))
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
