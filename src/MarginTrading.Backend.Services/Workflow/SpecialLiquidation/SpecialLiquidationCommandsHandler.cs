using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Service.ExchangeConnector.Client;
using Lykke.Service.ExchangeConnector.Client.Models;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.MatchingEngines;
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
        private readonly IExchangeConnectorService _exchangeConnectorService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly IAccountsCacheService _accountsCacheService;

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
            IExchangeConnectorService exchangeConnectorService,
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
            _exchangeConnectorService = exchangeConnectorService;
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

            //ensure idempotency
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
                        CausationOperationId = command.CausationOperationId
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

            if (_assetPairsCache.GetAssetPairById(command.Instrument).IsDiscontinued)
            {
                publisher.PublishEvent(new SpecialLiquidationFailedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = $"Asset pair {command.Instrument} is discontinued",
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
            
            //ensure idempotency
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

            if (executionInfo != null)
            {
                if (executionInfo.Data.State > SpecialLiquidationOperationState.PriceRequested)
                {
                    return CommandHandlingResult.Ok();
                }
                
                if (_dateService.Now() >= command.CreationTime.AddSeconds(command.TimeoutSeconds))
                {
                    if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.PriceRequested,
                        SpecialLiquidationOperationState.Failed))
                    {
                        publisher.PublishEvent(new SpecialLiquidationFailedEvent
                        {
                            OperationId = command.OperationId,
                            CreationTime = _dateService.Now(),
                            Reason = $"Timeout of {command.TimeoutSeconds} seconds from {command.CreationTime:s}",
                        });
                
                        _chaosKitty.Meow(command.OperationId);

                        await _operationExecutionInfoRepository.Save(executionInfo);
                    }

                    return CommandHandlingResult.Ok();
                }
            }

            return CommandHandlingResult.Fail(_marginTradingSettings.SpecialLiquidation.RetryTimeout);
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

            if (executionInfo == null)
            {
                return;
            }

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.PriceReceived,
                SpecialLiquidationOperationState.ExternalOrderExecuted))
            {
                var order = new OrderModel(
                    tradeType: command.Volume > 0 ? TradeType.Buy : TradeType.Sell,
                    orderType: OrderType.Market.ToType<Lykke.Service.ExchangeConnector.Client.Models.OrderType>(),
                    timeInForce: TimeInForce.FillOrKill,
                    volume: (double) Math.Abs(command.Volume),
                    dateTime: _dateService.Now(),
                    exchangeName: executionInfo.Data.ExternalProviderId,
                    instrument: command.Instrument,
                    price: (double?) command.Price,
                    orderId: _identityGenerator.GenerateAlphanumericId());
                
                try
                {
                    var executionResult = await _exchangeConnectorService.CreateOrderAsync(order);

                    publisher.PublishEvent(new SpecialLiquidationOrderExecutedEvent
                    {
                        OperationId = command.OperationId,
                        CreationTime = _dateService.Now(),
                        MarketMakerId = executionInfo.Data.ExternalProviderId,
                        ExecutionTime = executionResult.Time,
                        OrderId = executionResult.ExchangeOrderId,
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
                
                //todo think what if meow happens here

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        [UsedImplicitly]
        private async Task Handle(ExecuteSpecialLiquidationOrdersInternalCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: SpecialLiquidationSaga.OperationName,
                id: command.OperationId);

            if (executionInfo == null)
            {
                return;
            }

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.InternalOrderExecutionStarted,
                SpecialLiquidationOperationState.InternalOrdersExecuted))
            {
                try
                {
                    //close positions with the quote from gavel
                    //TODO think what if positions are liquidated partially, when exception is thrown
                    await _tradingEngine.LiquidatePositionsAsync(
                        me: new SpecialLiquidationMatchingEngine(command.Price, command.MarketMakerId,
                            command.ExternalOrderId, command.ExternalExecutionTime), 
                        positionIds: executionInfo.Data.PositionIds.ToArray(), 
                        correlationId: command.OperationId);
                
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

            if (executionInfo == null)
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