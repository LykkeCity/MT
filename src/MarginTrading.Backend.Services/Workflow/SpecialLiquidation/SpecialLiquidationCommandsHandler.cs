// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.ExchangeConnector;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Backend.Services.Workflow.Liquidation;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Events;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;
using OrderType = MarginTrading.Backend.Core.Orders.OrderType;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation
{
    [UsedImplicitly]
    public class SpecialLiquidationCommandsHandler
    {
        private readonly ITradingEngine _tradingEngine;
        private readonly IDateService _dateService;
        private readonly IOrderReader _orderReader;
        private readonly IChaosKitty _chaosKitty;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly ILog _log;
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IAssetPairDayOffService _assetPairDayOffService;
        private readonly IExchangeConnectorClient _exchangeConnectorClient;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly IAccountsCacheService _accountsCacheService;
        
        private const AccountLevel ValidAccountLevel = AccountLevel.StopOut;

        public SpecialLiquidationCommandsHandler(
            ITradingEngine tradingEngine,
            IDateService dateService,
            IOrderReader orderReader,
            IChaosKitty chaosKitty,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            ILog log,
            MarginTradingSettings marginTradingSettings,
            IAssetPairsCache assetPairsCache,
            IAssetPairDayOffService assetPairDayOffService,
            IExchangeConnectorClient exchangeConnectorClient,
            IIdentityGenerator identityGenerator,
            IAccountsCacheService accountsCacheService)
        {
            _tradingEngine = tradingEngine;
            _dateService = dateService;
            _orderReader = orderReader;
            _chaosKitty = chaosKitty;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _log = log;
            _marginTradingSettings = marginTradingSettings;
            _assetPairsCache = assetPairsCache;
            _assetPairDayOffService = assetPairDayOffService;
            _exchangeConnectorClient = exchangeConnectorClient;
            _identityGenerator = identityGenerator;
            _accountsCacheService = accountsCacheService;
        }
        
        [UsedImplicitly]
        private async Task Handle(StartSpecialLiquidationInternalCommand command, IEventPublisher publisher)
        {
            if (!_marginTradingSettings.SpecialLiquidation.Enabled)
            {
                publisher.PublishEvent(new SpecialLiquidationFailedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = "Special liquidation is disabled in settings",
                });
                
                return;
            }
            
            //validate the list of positions contain only the same instrument
            var positions = _orderReader.GetPositions().Where(x => command.PositionIds.Contains(x.Id)).ToList();

            if (!string.IsNullOrEmpty(command.AccountId))
            {
                if (_accountsCacheService.TryGet(command.AccountId) == null)
                {
                    publisher.PublishEvent(new SpecialLiquidationFailedEvent
                    {
                        OperationId = command.OperationId,
                        CreationTime = _dateService.Now(),
                        Reason = $"Account {command.AccountId} does not exist",
                    });
                    return;
                }

                positions = positions.Where(x => x.AccountId == command.AccountId).ToList();
            }
            
            if (positions.Select(x => x.AssetPairId).Distinct().Count() > 1)
            {
                publisher.PublishEvent(new SpecialLiquidationFailedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = "The list of positions is of different instruments",
                });
                
                return;
            }

            if (!positions.Any())
            {
                publisher.PublishEvent(new SpecialLiquidationFailedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = "No positions to liquidate",
                });
                
                return;
            }

            if (!TryGetExchangeNameFromPositions(positions, out var externalProviderId))
            {
                publisher.PublishEvent(new SpecialLiquidationFailedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = "All requested positions must be open on the same external exchange",
                });
                return;
            }

            var assetPairId = positions.First().AssetPairId;
            if (_assetPairsCache.GetAssetPairById(assetPairId).IsDiscontinued)
            {
                publisher.PublishEvent(new SpecialLiquidationFailedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = $"Asset pair {assetPairId} is discontinued",
                });
                
                return;
            }

            var executionInfo = await _operationExecutionInfoRepository.GetOrAddAsync(
                operationName: SpecialLiquidationSaga.OperationName,
                operationId: command.OperationId,
                factory: () => new OperationExecutionInfo<SpecialLiquidationOperationData>(
                    operationName: SpecialLiquidationSaga.OperationName,
                    id: command.OperationId,
                    lastModified: _dateService.Now(),
                    data: new SpecialLiquidationOperationData
                    {
                        State = SpecialLiquidationOperationState.Initiated,
                        Instrument = assetPairId,
                        PositionIds = positions.Select(x => x.Id).ToList(),
                        ExternalProviderId = externalProviderId,
                        AccountId = command.AccountId,
                        CausationOperationId = command.CausationOperationId,
                        AdditionalInfo = command.AdditionalInfo,
                        OriginatorType = command.OriginatorType
                    }
                ));

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.Initiated, SpecialLiquidationOperationState.Started))
            {
                publisher.PublishEvent(new SpecialLiquidationStartedInternalEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Instrument = positions.FirstOrDefault()?.AssetPairId,
                });
                
                _chaosKitty.Meow(command.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }
        
        [UsedImplicitly]
        private async Task Handle(StartSpecialLiquidationCommand command, IEventPublisher publisher)
        {   
            if (!_marginTradingSettings.SpecialLiquidation.Enabled)
            {
                publisher.PublishEvent(new SpecialLiquidationFailedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = "Special liquidation is disabled in settings",
                });
                
                return;
            }
            
            //Validate that market is closed for instrument .. only in ExchangeConnector == Real mode
            if (_marginTradingSettings.ExchangeConnector == ExchangeConnectorType.RealExchangeConnector 
                && !_assetPairDayOffService.IsDayOff(command.Instrument))
            {
                publisher.PublishEvent(new SpecialLiquidationFailedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = $"Asset pair {command.Instrument} market must be disabled to start Special Liquidation",
                });

                return;
            }

            var openedPositions = _orderReader.GetPositions().Where(x => x.AssetPairId == command.Instrument).ToList();

            if (!openedPositions.Any())
            {
                publisher.PublishEvent(new SpecialLiquidationFailedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = "No positions to liquidate",
                });
                return;
            }

            if (_assetPairsCache.GetAssetPairById(command.Instrument).IsDiscontinued)
            {
                await _log.WriteWarningAsync(
                    nameof(StartSpecialLiquidationCommand),
                    nameof(SpecialLiquidationCommandsHandler),
                    $"Position(s) {string.Join(", ", openedPositions.Select(x => x.Id))} should be closed for discontinued instrument {command.Instrument}.");
            }

            if (!TryGetExchangeNameFromPositions(openedPositions, out var externalProviderId))
            {
                publisher.PublishEvent(new SpecialLiquidationFailedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = "All requested positions must be open on the same external exchange",
                });
                return;
            }
            
            var executionInfo = await _operationExecutionInfoRepository.GetOrAddAsync(
                operationName: SpecialLiquidationSaga.OperationName,
                operationId: command.OperationId,
                factory: () => new OperationExecutionInfo<SpecialLiquidationOperationData>(
                    operationName: SpecialLiquidationSaga.OperationName,
                    id: command.OperationId,
                    lastModified: _dateService.Now(),
                    data: new SpecialLiquidationOperationData
                    {
                        State = SpecialLiquidationOperationState.Initiated,
                        Instrument = command.Instrument,
                        PositionIds = openedPositions.Select(x => x.Id).ToList(),
                        ExternalProviderId = externalProviderId,
                        OriginatorType = OriginatorType.System,
                        AdditionalInfo = LykkeConstants.LiquidationByCaAdditionalInfo,
                        RequestedFromCorporateActions = true
                    }
                ));

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.Initiated, SpecialLiquidationOperationState.Started))
            {
                publisher.PublishEvent(new SpecialLiquidationStartedInternalEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Instrument = command.Instrument,
                });
                
                _chaosKitty.Meow(command.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        [UsedImplicitly]
        private async Task<CommandHandlingResult> Handle(GetPriceForSpecialLiquidationTimeoutInternalCommand command,
            IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: SpecialLiquidationSaga.OperationName,
                id: command.OperationId);

            if (executionInfo?.Data != null)
            {
                if (executionInfo.Data.State > SpecialLiquidationOperationState.PriceRequested 
                    || executionInfo.Data.RequestNumber > command.RequestNumber)
                {
                    return CommandHandlingResult.Ok();
                }
                
                if (_dateService.Now() >= command.CreationTime.AddSeconds(command.TimeoutSeconds))
                {
                    if (executionInfo.Data.SwitchState(executionInfo.Data.State,
                        SpecialLiquidationOperationState.Failed))
                    {
                        publisher.PublishEvent(new SpecialLiquidationFailedEvent
                        {
                            OperationId = command.OperationId,
                            CreationTime = _dateService.Now(),
                            Reason = $"Timeout of {command.TimeoutSeconds} seconds from {command.CreationTime:s}",
                            CanRetryPriceRequest = true
                        });
                
                        _chaosKitty.Meow(command.OperationId);

                        await _operationExecutionInfoRepository.Save(executionInfo);
                    }

                    return CommandHandlingResult.Ok();
                }
            }

            return CommandHandlingResult.Fail(_marginTradingSettings.SpecialLiquidation.PriceRequestTimeoutCheckPeriod);
        }

        /// <summary>
        /// Special handler sends API request to close positions 
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(ExecuteSpecialLiquidationOrderCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: SpecialLiquidationSaga.OperationName,
                id: command.OperationId);

            if (executionInfo?.Data == null)
            {
                return;
            }

            await _log.WriteInfoAsync(nameof(SpecialLiquidationCommandsHandler),
                nameof(ExecuteSpecialLiquidationOrderCommand), 
                command.ToJson(), 
                "Checking if position special liquidation should be failed");
            if (!string.IsNullOrEmpty(executionInfo.Data.CausationOperationId))
            {
                await _log.WriteInfoAsync(nameof(SpecialLiquidationCommandsHandler),
                    nameof(ExecuteSpecialLiquidationOrderCommand), 
                    command.ToJson(), 
                    "Special liquidation is caused by regular liquidation, checking liquidation type.");
                
                var liquidationInfo = await _operationExecutionInfoRepository.GetAsync<LiquidationOperationData>(
                    operationName: LiquidationSaga.OperationName,
                    id: executionInfo.Data.CausationOperationId);

                if (liquidationInfo == null)
                {
                    await _log.WriteInfoAsync(nameof(SpecialLiquidationCommandsHandler),
                        nameof(ExecuteSpecialLiquidationOrderCommand), 
                        command.ToJson(), 
                        "Regular liquidation does not exist, position close will not be failed.");
                }
                else
                {
                    if (liquidationInfo.Data.LiquidationType == LiquidationType.Forced)
                    {
                        await _log.WriteInfoAsync(nameof(SpecialLiquidationCommandsHandler),
                            nameof(ExecuteSpecialLiquidationOrderCommand), 
                            command.ToJson(), 
                            "Regular liquidation type is Forced (Close All), position close will not be failed.");
                    }
                    else
                    {
                        var account = _accountsCacheService.Get(executionInfo.Data.AccountId);
                        if (account.GetAccountLevel() != ValidAccountLevel)
                        {
                            await _log.WriteWarningAsync(
                                nameof(SpecialLiquidationCommandsHandler),
                                nameof(ExecuteSpecialLiquidationOrderCommand),
                                new {accountId = account.Id, accountLevel = account.GetAccountLevel().ToString()}.ToJson(),
                                $"Unable to execute special liquidation since account level is not {ValidAccountLevel.ToString()}.");
                    
                            publisher.PublishEvent(new SpecialLiquidationFailedEvent
                            {
                                OperationId = command.OperationId,
                                CreationTime = _dateService.Now(),
                                Reason = $"Account level is not {ValidAccountLevel.ToString()}.",
                                CanRetryPriceRequest = false
                            });
                    
                            return;
                        }
                    }
                }
            }

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.PriceReceived,
                SpecialLiquidationOperationState.ExternalOrderExecuted))
            {
                if (command.Volume == 0)
                {
                    publisher.PublishEvent(new SpecialLiquidationOrderExecutedEvent
                    {
                        OperationId = command.OperationId,
                        CreationTime = _dateService.Now(),
                        MarketMakerId = "ZeroNetVolume",
                        ExecutionTime = _dateService.Now(),
                        OrderId = _identityGenerator.GenerateGuid(),
                        ExecutionPrice = command.Price
                    });
                }
                else
                {
                    var operationInfo = new TradeOperationInfo
                    {
                        OperationId = executionInfo.Id, 
                        RequestNumber = executionInfo.Data.RequestNumber
                    };

                    var order = new OrderModel(
                        tradeType: command.Volume > 0 ? TradeType.Buy : TradeType.Sell,
                        orderType: OrderType.Market.ToType<Contracts.ExchangeConnector.OrderType>(),
                        timeInForce: TimeInForce.FillOrKill,
                        volume: (double)Math.Abs(command.Volume),
                        dateTime: _dateService.Now(),
                        exchangeName: operationInfo.ToJson(), //hack, but ExchangeName is not used and we need this info
                                                              // TODO: create a separate field and remove hack (?)
                        instrument: command.Instrument,
                        price: (double?)command.Price,
                        orderId: _identityGenerator.GenerateAlphanumericId(),
                        modality: executionInfo.Data.RequestedFromCorporateActions ? TradeRequestModality.Liquidation_CorporateAction : TradeRequestModality.Liquidation_MarginCall);

                    try
                    {
                        var executionResult = await _exchangeConnectorClient.ExecuteOrder(order);

                        if (!executionResult.Success)
                        {
                            throw new Exception(
                                $"External order was not executed. Status: {executionResult.ExecutionStatus}. " +
                                $"Failure: {executionResult.FailureType}");
                        }

                        var executionPrice = (decimal) executionResult.Price == default
                            ? command.Price
                            : (decimal) executionResult.Price;

                        if (executionPrice.EqualsZero())
                        {
                            throw new Exception("Execution price is equal to 0.");
                        }
                        
                        publisher.PublishEvent(new SpecialLiquidationOrderExecutedEvent
                        {
                            OperationId = command.OperationId,
                            CreationTime = _dateService.Now(),
                            MarketMakerId = executionInfo.Data.ExternalProviderId,
                            ExecutionTime = executionResult.Time,
                            OrderId = executionResult.ExchangeOrderId,
                            ExecutionPrice = executionPrice
                        });
                    }
                    catch (Exception exception)
                    {
                        publisher.PublishEvent(new SpecialLiquidationOrderExecutionFailedEvent
                        {
                            OperationId = command.OperationId,
                            CreationTime = _dateService.Now(),
                            Reason = exception.Message
                        });
                        await _log.WriteWarningAsync(nameof(SpecialLiquidationCommandsHandler),
                            nameof(ExecuteSpecialLiquidationOrderCommand),
                            $"Failed to execute the order: {order.ToJson()}",
                            exception);
                    }
                }
                
                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        [UsedImplicitly]
        private async Task Handle(ExecuteSpecialLiquidationOrdersInternalCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: SpecialLiquidationSaga.OperationName,
                id: command.OperationId);

            if (executionInfo?.Data == null)
            {
                return;
            }

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.InternalOrderExecutionStarted,
                SpecialLiquidationOperationState.InternalOrdersExecuted))
            {
                try
                {
                    var modality = executionInfo.Data.RequestedFromCorporateActions
                        ? OrderModality.Liquidation_CorporateAction
                        : OrderModality.Liquidation_MarginCall;
                    
                    //close positions with the quotes from gavel
                    await _tradingEngine.LiquidatePositionsUsingSpecialWorkflowAsync(
                        me: new SpecialLiquidationMatchingEngine(command.Price, command.MarketMakerId,
                            command.ExternalOrderId, command.ExternalExecutionTime), 
                        positionIds: executionInfo.Data.PositionIds.ToArray(), 
                        correlationId: command.OperationId,
                        executionInfo.Data.AdditionalInfo,
                        executionInfo.Data.OriginatorType,
                        modality);
                
                    _chaosKitty.Meow(command.OperationId);
                    
                    publisher.PublishEvent(new SpecialLiquidationFinishedEvent
                    {
                        OperationId = command.OperationId,
                        CreationTime = _dateService.Now(),
                    });
                }
                catch (Exception ex)
                {
                    publisher.PublishEvent(new SpecialLiquidationFailedEvent
                    {
                        OperationId = command.OperationId,
                        CreationTime = _dateService.Now(),
                        Reason = ex.Message,
                        CanRetryPriceRequest = true
                    });
                }
                
                _chaosKitty.Meow(command.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        [UsedImplicitly]
        private async Task Handle(FailSpecialLiquidationInternalCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: SpecialLiquidationSaga.OperationName,
                id: command.OperationId);
            
            if (executionInfo?.Data == null)
            {
                return;
            }
            
            if (executionInfo.Data.SwitchState(executionInfo.Data.State,//from any state
                SpecialLiquidationOperationState.OnTheWayToFail))
            {
                publisher.PublishEvent(new SpecialLiquidationFailedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = command.Reason,
                });
                
                _chaosKitty.Meow(command.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        private bool TryGetExchangeNameFromPositions(IEnumerable<Position> positions, out string externalProviderId)
        {
            var externalProviderIds = positions.Select(x => x.ExternalProviderId).Distinct().ToList();
            if (externalProviderIds.Count != 1)
            {
                externalProviderId = null;
                return false;
            }

            externalProviderId = externalProviderIds.Single();
            return true;
        }
    }
}