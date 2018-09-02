using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Events;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation
{
    [UsedImplicitly]
    public class SpecialLiquidationCommandsHandler
    {
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly ITradingEngine _tradingEngine;
        private readonly IDateService _dateService;
        private readonly IOrderReader _orderReader;
        private readonly IChaosKitty _chaosKitty;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly ILog _log;
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly IAssetPairDayOffService _assetPairDayOffService;

        public SpecialLiquidationCommandsHandler(
            IAssetPairsCache assetPairsCache,
            ITradingEngine tradingEngine,
            IDateService dateService,
            IOrderReader orderReader,
            IChaosKitty chaosKitty,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            ILog log,
            MarginTradingSettings marginTradingSettings,
            IAssetPairDayOffService assetPairDayOffService)
        {
            _assetPairsCache = assetPairsCache;
            _tradingEngine = tradingEngine;
            _dateService = dateService;
            _orderReader = orderReader;
            _chaosKitty = chaosKitty;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _log = log;
            _marginTradingSettings = marginTradingSettings;
            _assetPairDayOffService = assetPairDayOffService;
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
                        Instrument = positions.FirstOrDefault()?.AssetPairId,
                        PositionIds = command.PositionIds.ToList(),
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

            var openedPositions = _orderReader.GetPositions().Where(x => x.AssetPairId == command.Instrument)
                .Select(x => x.Id).ToList();
            
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
                        PositionIds = openedPositions
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

        private async Task<CommandHandlingResult> Handle(GetPriceForSpecialLiquidationTimeoutInternalCommand command,
            IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: SpecialLiquidationSaga.OperationName,
                id: command.OperationId);

            if (executionInfo != null)
            {
                if (executionInfo.Data.State >= SpecialLiquidationOperationState.PriceRequested)
                {
                    return CommandHandlingResult.Ok();
                }
                
                if (_dateService.Now() > command.CreationTime.AddSeconds(command.TimeoutSeconds))
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

                        await _operationExecutionInfoRepository.Save(executionInfo);
                    }

                    return CommandHandlingResult.Ok();
                }
            }

            return CommandHandlingResult.Fail(_marginTradingSettings.SpecialLiquidation.RetryTimeout);
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

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.ExternalOrderExecuted,
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
    }
}