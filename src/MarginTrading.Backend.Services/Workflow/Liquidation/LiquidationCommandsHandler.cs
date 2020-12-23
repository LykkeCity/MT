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
using Lykke.Common.Log;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.Workflow.Liquidation;
using MarginTrading.Backend.Contracts.Workflow.Liquidation.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Helpers;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;
using MarginTrading.Backend.Services.Workflow.Liquidation.Events;
using MarginTrading.Common.Extensions;
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
        private readonly ITradingEngine _tradingEngine;
        private readonly OrdersCache _ordersCache;
        private readonly ILog _log;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly IEventChannel<LiquidationEndEventArgs> _liquidationEndEventChannel;
        private readonly LiquidationHelper _liquidationHelper;
        private readonly ILiquidationFailureExecutor _failureExecutor;

        private const AccountLevel ValidAccountLevel = AccountLevel.StopOut;

        public LiquidationCommandsHandler(
            IAccountsCacheService accountsCache,
            IDateService dateService,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            IChaosKitty chaosKitty,
            ITradingEngine tradingEngine,
            OrdersCache ordersCache,
            ILog log,
            IAccountUpdateService accountUpdateService,
            IEventChannel<LiquidationEndEventArgs> liquidationEndEventChannel,
            LiquidationHelper liquidationHelper, 
            ILiquidationFailureExecutor failureExecutor)
        {
            _accountsCache = accountsCache;
            _dateService = dateService;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _chaosKitty = chaosKitty;
            _tradingEngine = tradingEngine;
            _ordersCache = ordersCache;
            _log = log;
            _accountUpdateService = accountUpdateService;
            _liquidationEndEventChannel = liquidationEndEventChannel;
            _liquidationHelper = liquidationHelper;
            _failureExecutor = failureExecutor;
        }

        [UsedImplicitly]
        public async Task Handle(StartLiquidationInternalCommand command, 
            IEventPublisher publisher)
        {
            #region Private Methods
            
            void PublishFailedEvent(string reason)
            {
                publisher.PublishEvent(new LiquidationFailedEvent
                {
                    OperationId = command.OperationId, 
                    CreationTime = _dateService.Now(),
                    Reason = reason,
                    LiquidationType = command.LiquidationType.ToType<LiquidationTypeContract>(),
                    AccountId = command.AccountId,
                    AssetPairId = command.AssetPairId,
                    Direction = command.Direction?.ToType<PositionDirectionContract>(),
                });
            
                _liquidationEndEventChannel.SendEvent(this, new LiquidationEndEventArgs
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    AccountId = command.AccountId,
                    LiquidatedPositionIds = new List<string>(),
                    FailReason = reason,
                });
            }
            
            #endregion
            
            #region Validations

            if (string.IsNullOrEmpty(command.AccountId))
            {
                PublishFailedEvent("AccountId must be specified");
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
                        ProcessedPositionIds = new List<string>(),
                        LiquidationType = command.LiquidationType,
                        OriginatorType = command.OriginatorType,
                        AdditionalInfo = command.AdditionalInfo,
                        StartedAt = command.CreationTime
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
                
                publisher.PublishEvent(new LiquidationStartedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    AssetPairId = executionInfo.Data.AssetPairId,
                    AccountId = executionInfo.Data.AccountId,
                    LiquidationType = executionInfo.Data.LiquidationType.ToType<LiquidationTypeContract>()
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
                $"Liquidation [{command.OperationId}] failed ({command.Reason})", command.OperationId,
                executionInfo.Data.LiquidationType);
            
            _chaosKitty.Meow(
                $"{nameof(FailLiquidationInternalCommand)}:" +
                $"Publish_LiquidationFailedInternalEvent:" +
                $"{command.OperationId}");
            
            publisher.PublishEvent(new LiquidationFailedEvent
            {
                OperationId = command.OperationId,
                CreationTime = _dateService.Now(),
                Reason = command.Reason,
                LiquidationType = command.LiquidationType.ToType<LiquidationTypeContract>(),
                AccountId = executionInfo.Data.AccountId,
                AssetPairId = executionInfo.Data.AssetPairId,
                Direction = executionInfo.Data.Direction?.ToType<PositionDirectionContract>(),
                QuoteInfo = executionInfo.Data.QuoteInfo,
                ProcessedPositionIds = executionInfo.Data.ProcessedPositionIds,
                LiquidatedPositionIds = executionInfo.Data.LiquidatedPositionIds,
                OpenPositionsRemainingOnAccount = _ordersCache.Positions.GetPositionsByAccountIds(executionInfo.Data.AccountId).Count,
                CurrentTotalCapital = _accountsCache.Get(executionInfo.Data.AccountId).GetTotalCapital(),
            });
            
            _liquidationEndEventChannel.SendEvent(this, new LiquidationEndEventArgs
            {
                OperationId = command.OperationId,
                CreationTime = _dateService.Now(),
                AccountId = executionInfo.Data.AccountId,
                LiquidatedPositionIds = executionInfo.Data.LiquidatedPositionIds,
                FailReason = command.Reason,
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
                $"Liquidation [{command.OperationId}] finished ({command.Reason})", command.OperationId,
                executionInfo.Data.LiquidationType);
            
            _chaosKitty.Meow(
                $"{nameof(FinishLiquidationInternalCommand)}:" +
                $"Publish_LiquidationFinishedInternalEvent:" +
                $"{command.OperationId}");
            
            publisher.PublishEvent(new LiquidationFinishedEvent
            {
                OperationId = command.OperationId,
                CreationTime = _dateService.Now(),
                LiquidationType = command.LiquidationType.ToType<LiquidationTypeContract>(),
                AccountId = executionInfo.Data.AccountId,
                AssetPairId = executionInfo.Data.AssetPairId,
                Direction = executionInfo.Data.Direction?.ToType<PositionDirectionContract>(),
                QuoteInfo = executionInfo.Data.QuoteInfo,
                ProcessedPositionIds = command.ProcessedPositionIds,
                LiquidatedPositionIds = command.LiquidatedPositionIds,
                OpenPositionsRemainingOnAccount = _ordersCache.Positions.GetPositionsByAccountIds(executionInfo.Data.AccountId).Count,
                CurrentTotalCapital = _accountsCache.Get(executionInfo.Data.AccountId).GetTotalCapital(),
            });
            
            _liquidationEndEventChannel.SendEvent(this, new LiquidationEndEventArgs
            {
                OperationId = command.OperationId,
                CreationTime = _dateService.Now(),
                AccountId = executionInfo.Data.AccountId,
                LiquidatedPositionIds = command.LiquidatedPositionIds,
            });
        }
        
        [UsedImplicitly]
        public async Task Handle(LiquidatePositionsInternalCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<LiquidationOperationData>(
                operationName: LiquidationSaga.OperationName,
                id: command.OperationId);

            if (executionInfo?.Data == null)
            {
                return;
            }

            await _log.WriteInfoAsync(nameof(LiquidationCommandsHandler),
                nameof(LiquidatePositionsInternalCommand), 
                command.ToJson(), 
                "Checking if position liquidation should be failed");
            var account = _accountsCache.Get(executionInfo.Data.AccountId);
            if (ShouldFailExecution(account.GetAccountLevel(), executionInfo.Data.LiquidationType))
            {
                await _log.WriteWarningAsync(
                    nameof(LiquidationCommandsHandler),
                    nameof(LiquidatePositionsInternalCommand),
                    new {accountId = account.Id, accountLevel = account.GetAccountLevel().ToString()}.ToJson(),
                    $"Unable to liquidate positions since account level is not {ValidAccountLevel.ToString()}.");

                await _failureExecutor.ExecuteAsync(publisher,
                    account.Id,
                    command.OperationId,
                    $"Account level is not {ValidAccountLevel.ToString()}.");

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

            if (!_liquidationHelper.CheckIfNetVolumeCanBeLiquidated(command.AssetPairId, positions, out var details))
            {
                publisher.PublishEvent(new NotEnoughLiquidityInternalEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    PositionIds = command.PositionIds,
                    Details = details
                });
                return;
            }

            var liquidationInfos = new List<LiquidationInfo>();

            var comment = string.Empty;

            switch (executionInfo.Data.LiquidationType)
            {
                case LiquidationType.Mco:
                    comment = "MCO liquidation";
                    break;
                
                case LiquidationType.Normal:
                    comment = "Liquidation";
                    break;
                
                case LiquidationType.Forced:
                    comment = "Close positions group";
                    break;
            }
            
            var positionGroups = positions
                .GroupBy(p => (p.AssetPairId, p.AccountId, p.Direction, p
                    .OpenMatchingEngineId, p.ExternalProviderId, p.EquivalentAsset))
                .Select(gr => new PositionsCloseData(
                    gr.ToList(),
                    gr.Key.AccountId,
                    gr.Key.AssetPairId,
                    gr.Key.OpenMatchingEngineId,
                    gr.Key.ExternalProviderId,
                    executionInfo.Data.OriginatorType,
                    executionInfo.Data.AdditionalInfo,
                    command.OperationId,
                    gr.Key.EquivalentAsset,
                    comment));

            foreach (var positionGroup in positionGroups)
            {
                try
                {
                    var (result, order) = await _tradingEngine.ClosePositionsAsync(positionGroup, false);

                    foreach (var position in positionGroup.Positions)
                    {
                        liquidationInfos.Add(new LiquidationInfo
                        {
                            PositionId = position.Id,
                            IsLiquidated = true,
                            Comment = order != null ? $"Order: {order.Id}" : result.ToString()
                        });
                    }
                }
                catch (Exception ex)
                {
                    await _log.WriteWarningAsync(nameof(LiquidationCommandsHandler),
                        nameof(LiquidatePositionsInternalCommand),
                        $"Failed to close positions {string.Join(",", positionGroup.Positions.Select(p => p.Id))} on liquidation operation #{command.OperationId}",
                        ex);

                    foreach (var position in positionGroup.Positions)
                    {
                        liquidationInfos.Add(new LiquidationInfo
                        {
                            PositionId = position.Id,
                            IsLiquidated = false,
                            Comment = $"Close position failed: {ex.Message}"
                        });
                    }
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

            await _log.WriteInfoAsync(nameof(LiquidationCommandsHandler),
                nameof(ResumeLiquidationInternalCommand), 
                command.ToJson(), 
                "Checking if position liquidation should be failed");
            var account = _accountsCache.Get(executionInfo.Data.AccountId);
            if (ShouldFailExecution(account.GetAccountLevel(), executionInfo.Data.LiquidationType))
            {
                await _log.WriteWarningAsync(
                    nameof(LiquidationCommandsHandler),
                    nameof(ResumeLiquidationInternalCommand),
                    new {accountId = account.Id, accountLevel = account.GetAccountLevel().ToString()}.ToJson(),
                    $"Unable to resume liquidation since account level is not {ValidAccountLevel.ToString()}.");
                
                await _failureExecutor.ExecuteAsync(publisher, 
                    account.Id, 
                    command.OperationId,
                    $"Account level is not {ValidAccountLevel.ToString()}.");

                return;
            }
            
            if (!command.IsCausedBySpecialLiquidation &&
                (!command.ResumeOnlyFailed || executionInfo.Data.State == LiquidationOperationState.Failed) ||
                executionInfo.Data.State == LiquidationOperationState.SpecialLiquidationStarted)
            {
                publisher.PublishEvent(new LiquidationResumedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    Comment = command.Comment,
                    IsCausedBySpecialLiquidation = command.IsCausedBySpecialLiquidation,
                    PositionsLiquidatedBySpecialLiquidation = command.PositionsLiquidatedBySpecialLiquidation,
                    AccountId = executionInfo.Data.AccountId,
                    AssetPairId = executionInfo.Data.AssetPairId,
                    LiquidationType = executionInfo.Data.LiquidationType.ToType<LiquidationTypeContract>()
                });
            }
            else
            {
                await _log.WriteWarningAsync(
                    nameof(LiquidationCommandsHandler),
                    nameof(ResumeLiquidationInternalCommand),
                    null,
                    $"Unable to resume liquidation in state {executionInfo.Data.State}. Command: {command.ToJson()}");
            }
        }

        private bool ShouldFailExecution(AccountLevel accountLevel, LiquidationType liquidationType)
        {
            return accountLevel != ValidAccountLevel && liquidationType != LiquidationType.Forced;
        }
    }
}