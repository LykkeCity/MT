using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;
using MarginTrading.Backend.Services.Workflow.Liquidation.Events;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow.Liquidation
{
    [UsedImplicitly]
    public class LiquidationCommandsHandler
    {
        private readonly IAccountsCacheService _accountsCache;
        private readonly IDateService _dateService;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly IChaosKitty _chaosKitty;
        private readonly IMatchingEngineRouter _matchingEngineRouter;
        private readonly ITradingEngine _tradingEngine;
        private readonly OrdersCache _ordersCache;
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly ILog _log;
        private readonly IAccountUpdateService _accountUpdateService;

        public LiquidationCommandsHandler(IAccountsCacheService accountsCache,
            IDateService dateService,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            IChaosKitty chaosKitty, 
            IMatchingEngineRouter matchingEngineRouter,
            ITradingEngine tradingEngine,
            OrdersCache ordersCache,
            MarginTradingSettings marginTradingSettings,
            IAssetPairsCache assetPairsCache,
            ICfdCalculatorService cfdCalculatorService,
            ILog log,
            IAccountUpdateService accountUpdateService)
        {
            _accountsCache = accountsCache;
            _dateService = dateService;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _chaosKitty = chaosKitty;
            _matchingEngineRouter = matchingEngineRouter;
            _tradingEngine = tradingEngine;
            _ordersCache = ordersCache;
            _marginTradingSettings = marginTradingSettings;
            _assetPairsCache = assetPairsCache;
            _cfdCalculatorService = cfdCalculatorService;
            _log = log;
            _accountUpdateService = accountUpdateService;
        }

        [UsedImplicitly]
        public async Task Handle(StartLiquidationInternalCommand command, 
            IEventPublisher publisher)
        {
            
            #region Private Methods
            
            void PublishFailedEvent(string reason)
            {
                publisher.PublishEvent(new LiquidationFailedInternalEvent
                {
                    OperationId = command.OperationId, 
                    CreationTime = _dateService.Now(),
                    Reason = reason
                });
            }
            
            #endregion
            
            #region Validations

            if (string.IsNullOrEmpty(command.AccountId))
            {
                PublishFailedEvent("AccountId must be specified");
                return;
            }

            if (string.IsNullOrWhiteSpace(command.AssetPairId) && command.Direction.HasValue ||
                !string.IsNullOrWhiteSpace(command.AssetPairId) && !command.Direction.HasValue)
            {
                PublishFailedEvent("Both AssetPairId and Direction must be specified or empty");
                return;
            }
            
            if (_accountsCache.TryGet(command.AccountId) == null)
            {
                PublishFailedEvent( "Account does not exist");
                return;
            }
            
            #endregion
            
            var executionInfo = await _operationExecutionInfoRepository.GetOrAddAsync(
                operationName: LiquidationSaga.OperationName,
                operationId: command.OperationId,
                factory: () => new OperationExecutionInfo<LiquidationOperationData>(
                    operationName: LiquidationSaga.OperationName,
                    id: command.OperationId,
                    lastModified: _dateService.Now(),
                    data: new LiquidationOperationData
                    {
                        State = LiquidationOperationState.Initiated,
                        AccountId = command.AccountId,
                        AssetPairId = command.AssetPairId,
                        QuoteInfo = command.QuoteInfo,
                        Direction = command.Direction,
                        LiquidatedPositionIds = new List<string>(),
                        ProcessedPositionIds = new List<string>()
                    }
                ));
            
            if (executionInfo.Data.State == LiquidationOperationState.Initiated)
            {
                if (!_accountsCache.TryStartLiquidation(command.AccountId, command.OperationId,
                    out var currentOperationId))
                {
                    if (currentOperationId != command.OperationId)
                    {
                        PublishFailedEvent(
                            $"Liquidation is already in progress. Initiated by operation : {currentOperationId}");
                        return;
                    }
                }

                _chaosKitty.Meow(
                    $"{nameof(StartLiquidationInternalCommand)}:" +
                    $"Publish_LiquidationStartedInternalEvent:" +
                    $"{command.OperationId}");
                
                publisher.PublishEvent(new LiquidationStartedInternalEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now()
                });
            }
        }

        [UsedImplicitly]
        public async Task Handle(FailLiquidationInternalCommand command,
            IEventPublisher publisher)
        {
            
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<LiquidationOperationData>(
                operationName: LiquidationSaga.OperationName,
                id: command.OperationId);

            if (executionInfo?.Data == null)
            {
                return;
            }
            
            _accountUpdateService.RemoveLiquidationStateIfNeeded(executionInfo.Data.AccountId,
                $"Liquidation [{command.OperationId}] failed ({command.Reason})", command.OperationId);
            
            _chaosKitty.Meow(
                $"{nameof(FailLiquidationInternalCommand)}:" +
                $"Publish_LiquidationFailedInternalEvent:" +
                $"{command.OperationId}");
            
            publisher.PublishEvent(new LiquidationFailedInternalEvent
            {
                OperationId = command.OperationId,
                CreationTime = _dateService.Now(),
                Reason = command.Reason
            });
        }
        
        [UsedImplicitly]
        public async Task Handle(FinishLiquidationInternalCommand command,
            IEventPublisher publisher)
        {
            
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<LiquidationOperationData>(
                operationName: LiquidationSaga.OperationName,
                id: command.OperationId);

            if (executionInfo?.Data == null)
            {
                return;
            }
            
            _accountUpdateService.RemoveLiquidationStateIfNeeded(executionInfo.Data.AccountId,
                $"Liquidation [{command.OperationId}] finished ({command.Reason})", command.OperationId);
            
            _chaosKitty.Meow(
                $"{nameof(FinishLiquidationInternalCommand)}:" +
                $"Publish_LiquidationFinishedInternalEvent:" +
                $"{command.OperationId}");
            
            publisher.PublishEvent(new LiquidationFinishedInternalEvent
            {
                OperationId = command.OperationId,
                CreationTime = _dateService.Now()
            });
        }
        
        [UsedImplicitly]
        public async Task Handle(LiquidatePositionsInternalCommand command,
            IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<LiquidationOperationData>(
                operationName: LiquidationSaga.OperationName,
                id: command.OperationId);

            if (executionInfo?.Data == null)
            {
                return;
            }

            var positions = _ordersCache.Positions
                .GetPositionsByAccountIds(executionInfo.Data.AccountId)
                .Where(p => command.PositionIds.Contains(p.Id))
                .ToArray();

            if (!positions.Any())
            {
                publisher.PublishEvent(new PositionsLiquidationFinishedInternalEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    LiquidationInfos = command.PositionIds.Select(p =>
                        new LiquidationInfo
                        {
                            PositionId = p,
                            IsLiquidated = false,
                            Comment = "Opened position was not found"
                        }).ToList()
                });
                return;
            }

            if (!CheckIfNetVolumeCanBeLiquidated(command.AssetPairId, positions, out var additionalInfo))
            {
                publisher.PublishEvent(new NotEnoughLiquidityInternalEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    PositionIds = command.PositionIds,
                    AdditionalInfo = additionalInfo
                });
                return;
            }
            
            var liquidationInfos = new List<LiquidationInfo>();

            foreach (var position in positions)
            {
                try
                {
                    var order = await _tradingEngine.ClosePositionAsync(position.Id, OriginatorType.System, string.Empty,
                        command.OperationId, "Liquidation");

                    if (order.Status != OrderStatus.Executed && order.Status != OrderStatus.ExecutionStarted)
                    {
                        throw new Exception(order.RejectReasonText);
                    }

                    liquidationInfos.Add(new LiquidationInfo
                    {
                        PositionId = position.Id,
                        IsLiquidated = true,
                        Comment = $"Order: {order.Id}"
                    });
                }
                catch (Exception ex)
                {
                    await _log.WriteWarningAsync(nameof(LiquidationCommandsHandler), nameof(LiquidatePositionsInternalCommand),
                        $"Failed to close position {position.Id} on liquidation operation #{command.OperationId}", ex);
                    
                    liquidationInfos.Add(new LiquidationInfo
                    {
                        PositionId = position.Id,
                        IsLiquidated = false,
                        Comment = $"Close position failed: {ex.Message}"
                    });
                }
            }

            publisher.PublishEvent(new PositionsLiquidationFinishedInternalEvent
            {
                OperationId = command.OperationId,
                CreationTime = _dateService.Now(),
                LiquidationInfos = liquidationInfos
            });
        }

        [UsedImplicitly]
        public async Task Handle(ResumeLiquidationInternalCommand command,
            IEventPublisher publisher)
        {
            
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<LiquidationOperationData>(
                operationName: LiquidationSaga.OperationName,
                id: command.OperationId);

            if (executionInfo?.Data == null)
            {
                await _log.WriteWarningAsync(nameof(LiquidationCommandsHandler),
                    nameof(ResumeLiquidationInternalCommand),
                    $"Unable to resume liquidation. Execution info was not found. Command: {command.ToJson()}");
                return;
            }

            if (!command.IsCausedBySpecialLiquidation || 
                executionInfo.Data.State == LiquidationOperationState.SpecialLiquidationStarted)
            {
                publisher.PublishEvent(new LiquidationResumedInternalEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Comment = command.Comment,
                    IsCausedBySpecialLiquidation = command.IsCausedBySpecialLiquidation
                });
            }
            else
            {
                await _log.WriteWarningAsync(nameof(LiquidationCommandsHandler),
                    nameof(ResumeLiquidationInternalCommand),
                    $"Unable to resume liquidation in state {executionInfo.Data.State}. Command: {command.ToJson()}");
            }
        }
        
        
        #region Private methods
        
        private bool CheckIfNetVolumeCanBeLiquidated(string assetPairId, Position[] positions, out string additionalInfo)
        {
            var netPositionVolume = positions.Sum(p => p.Volume);

            var volumeInThresholdCurrency = GetVolumeInThresholdCurrency(netPositionVolume, assetPairId);

            if (_marginTradingSettings.SpecialLiquidation.VolumeThreshold > 0 &&
                volumeInThresholdCurrency.HasValue &&
                Math.Abs(volumeInThresholdCurrency.Value) > _marginTradingSettings.SpecialLiquidation.VolumeThreshold)
            {
                additionalInfo = $"Threshold exceeded. Net volume : {netPositionVolume}. " +
                                 $"Net volume in threshold currency : {volumeInThresholdCurrency}. " +
                                 $"Threshold : {_marginTradingSettings.SpecialLiquidation.VolumeThreshold}. " +
                                 $"Threshold currency : {_marginTradingSettings.SpecialLiquidation.VolumeThresholdCurrency}.";
                return false;
            }

            //TODO: discuss and handle situation with different MEs for different positions
            //at the current moment all positions has the same asset pair
            //and every asset pair can be processed only by one ME
            var anyPosition = positions.First();
            var me = _matchingEngineRouter.GetMatchingEngineForClose(anyPosition);
            //the same for externalProvider.. 
            var externalProvider = anyPosition.ExternalProviderId;

            if (me.GetPriceForClose(assetPairId, netPositionVolume, externalProvider) == null)
            {
                additionalInfo = $"Not enough depth of orderbook. Net volume : {netPositionVolume}.";
                return false;
            }

            additionalInfo = string.Empty;
            return true;
        }

        private decimal? GetVolumeInThresholdCurrency(decimal netVolumeToExecute, string assetPairId)
        {
            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(assetPairId);

            if (assetPair == null)
            {
                return null;
            }

            try
            {
                var quote = _cfdCalculatorService.GetQuoteRateForQuoteAsset(
                    _marginTradingSettings.SpecialLiquidation.VolumeThresholdCurrency,
                    assetPairId, assetPair.LegalEntity);

                return quote * netVolumeToExecute;
            }
            catch (Exception e)
            {
                _log.WriteError("Get net position volume in special liquidation threshold",
                    (netVolumeToExecute, assetPair,
                        _marginTradingSettings?.SpecialLiquidation?.VolumeThresholdCurrency),
                    e);
            }

            return null;
        }
        
        #endregion

    }
}