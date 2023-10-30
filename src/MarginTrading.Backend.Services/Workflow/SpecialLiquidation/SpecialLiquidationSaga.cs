// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Rfq;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Helpers;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Events;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation
{
    [UsedImplicitly]
    public class SpecialLiquidationSaga
    {
        private readonly IDateService _dateService;
        private readonly IChaosKitty _chaosKitty;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly IRfqService _specialLiquidationService;
        private readonly LiquidationHelper _liquidationHelper;
        private readonly OrdersCache _ordersCache;
        private readonly IRfqPauseService _rfqPauseService;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly ILog _log;
        
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly CqrsContextNamesSettings _cqrsContextNamesSettings;

        public const string OperationName = "SpecialLiquidation";

        public SpecialLiquidationSaga(
            IDateService dateService,
            IChaosKitty chaosKitty,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            IRfqService specialLiquidationService,
            MarginTradingSettings marginTradingSettings,
            CqrsContextNamesSettings cqrsContextNamesSettings,
            LiquidationHelper liquidationHelper,
            OrdersCache ordersCache,
            IRfqPauseService rfqPauseService,
            ILog log,
            IAssetPairsCache assetPairsCache)
        {
            _dateService = dateService;
            _chaosKitty = chaosKitty;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _specialLiquidationService = specialLiquidationService;
            _marginTradingSettings = marginTradingSettings;
            _cqrsContextNamesSettings = cqrsContextNamesSettings;
            _liquidationHelper = liquidationHelper;
            _ordersCache = ordersCache;
            _rfqPauseService = rfqPauseService;
            _log = log;
            _assetPairsCache = assetPairsCache;
        }

        [UsedImplicitly]
        private async Task Handle(SpecialLiquidationStartedInternalEvent e, ICommandSender sender)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: OperationName,
                id: e.OperationId);
            
            if (executionInfo?.Data == null)
            {
                return;
            }

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.Started, 
                SpecialLiquidationOperationState.PriceRequested))
            {
                executionInfo.Data.UpdatePositions(_ordersCache.GetPositions());
                executionInfo.Data.Instrument = e.Instrument;
                executionInfo.Data.RequestNumber = 1;
                executionInfo.Data.TryStartClosing(id => _ordersCache.Positions.GetPositionById(id), _dateService.Now);

                RequestPrice(sender, executionInfo);
                
                _chaosKitty.Meow(e.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        [UsedImplicitly]
        private async Task Handle(PriceForSpecialLiquidationCalculatedEvent e, ICommandSender sender)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: OperationName,
                id: e.OperationId);

            if (executionInfo?.Data == null)
                return;

            var pause = await _rfqPauseService.GetCurrentAsync(e.OperationId);
            if (pause?.State == PauseState.Active)
                return;

            if (!executionInfo.Data.SwitchState(SpecialLiquidationOperationState.PriceRequested,
                    SpecialLiquidationOperationState.PriceReceived))
                return;

            //validate that volume didn't change to peek either to execute order or request the price again
            var volumeChanged = executionInfo.Data.UpdatePositions(_ordersCache.GetPositions());
            if (volumeChanged)
            {
                // if RFQ is paused we will not continue
                var pauseAcknowledged = await _rfqPauseService.AcknowledgeAsync(e.OperationId);
                if (pauseAcknowledged) return;

                executionInfo.Data.NextRequestNumber();

                RequestPrice(sender, executionInfo);

                //switch state back, because we requested the price again and should handle in correctly when received
                executionInfo.Data.State = SpecialLiquidationOperationState.PriceRequested;

                await _operationExecutionInfoRepository.Save(executionInfo);

                //wait for the new price
                return;
            }

            await _rfqPauseService.StopPendingAsync(e.OperationId, PauseCancellationSource.PriceReceived,
                nameof(SpecialLiquidationSaga));

            executionInfo.Data.Price = e.Price;

            //execute order in Gavel by API
            sender.SendCommand(new ExecuteSpecialLiquidationOrderCommand
            {
                OperationId = e.OperationId,
                CreationTime = _dateService.Now(),
                Instrument = executionInfo.Data.Instrument,
                Volume = executionInfo.Data.Volume,
                Price = e.Price,
            }, _cqrsContextNamesSettings.TradingEngine);

            _chaosKitty.Meow(e.OperationId);

            await _operationExecutionInfoRepository.Save(executionInfo);
        }

        [UsedImplicitly]
        private async Task Handle(PriceForSpecialLiquidationCalculationFailedEvent e, ICommandSender sender)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: OperationName,
                id: e.OperationId);

            if (executionInfo?.Data == null)
                return;

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.PriceRequested,
                SpecialLiquidationOperationState.OnTheWayToFail))
            {
                sender.SendCommand(new FailSpecialLiquidationInternalCommand
                {
                    OperationId = e.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = e.Reason
                }, _cqrsContextNamesSettings.TradingEngine);
                
                _chaosKitty.Meow(e.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        [UsedImplicitly]
        private async Task Handle(SpecialLiquidationOrderExecutedEvent e, ICommandSender sender)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: OperationName,
                id: e.OperationId);

            if (executionInfo?.Data == null)
                return;
            
            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.ExternalOrderExecuted,
                SpecialLiquidationOperationState.InternalOrderExecutionStarted))
            {
                sender.SendCommand(new ExecuteSpecialLiquidationOrdersInternalCommand
                {
                    OperationId = e.OperationId,
                    CreationTime = _dateService.Now(),
                    Instrument = executionInfo.Data.Instrument,
                    Volume = executionInfo.Data.Volume,
                    Price = e.ExecutionPrice,
                    MarketMakerId = e.MarketMakerId,
                    ExternalOrderId = e.OrderId,
                    ExternalExecutionTime = e.ExecutionTime,
                }, _cqrsContextNamesSettings.TradingEngine);
                
                _chaosKitty.Meow(e.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            } 
        }

        [UsedImplicitly]
        private async Task Handle(SpecialLiquidationOrderExecutionFailedEvent e, ICommandSender sender)
        { 
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: OperationName,
                id: e.OperationId);
            
            if (executionInfo?.Data == null)
                return;

            if (PriceRequestRetryRequired(executionInfo.Data.RequestedFromCorporateActions))
            {
                var isDiscontinued = await FailIfInstrumentDiscontinued(executionInfo, sender);
                if (isDiscontinued) return;

                if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.ExternalOrderExecuted,
                        SpecialLiquidationOperationState.PriceRequested))
                {
                    await InternalRetryPriceRequest(e.CreationTime, sender, executionInfo,
                        _marginTradingSettings.SpecialLiquidation.PriceRequestRetryTimeout.Value);

                    return;
                }
            }

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.ExternalOrderExecuted,
                SpecialLiquidationOperationState.OnTheWayToFail))
            {
                sender.SendCommand(new FailSpecialLiquidationInternalCommand
                {
                    OperationId = e.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = e.Reason
                }, _cqrsContextNamesSettings.TradingEngine);
                
                _chaosKitty.Meow(e.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        [UsedImplicitly]
        private async Task Handle(SpecialLiquidationFinishedEvent e, ICommandSender sender)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: OperationName,
                id: e.OperationId);

            if (executionInfo?.Data == null)
                return;
            
            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.InternalOrdersExecuted,
                SpecialLiquidationOperationState.Finished))
            {
                if (!string.IsNullOrEmpty(executionInfo.Data.CausationOperationId))
                {
                    sender.SendCommand(new ResumeLiquidationInternalCommand
                    {
                        OperationId = executionInfo.Data.CausationOperationId,
                        CreationTime = _dateService.Now(),
                        Comment = $"Resume after special liquidation {executionInfo.Id} finished",
                        IsCausedBySpecialLiquidation = true,
                        CausationOperationId = executionInfo.Id,
                        PositionsLiquidatedBySpecialLiquidation = executionInfo.Data.PositionIds
                    }, _cqrsContextNamesSettings.TradingEngine);
                }
                
                _chaosKitty.Meow(e.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        [UsedImplicitly]
        private async Task Handle(SpecialLiquidationFailedEvent e, ICommandSender sender)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: OperationName,
                id: e.OperationId);
            
            if (executionInfo?.Data == null)
                return;

            if (e.CanRetryPriceRequest)
            {
                if (!executionInfo.Data.RequestedFromCorporateActions)
                {
                    var positions = _ordersCache.Positions
                        .GetPositionsByAccountIds(executionInfo.Data.AccountId)
                        .Where(p => executionInfo.Data.PositionIds.Contains(p.Id))
                        .ToArray();

                    if (_liquidationHelper.CheckIfNetVolumeCanBeLiquidated(executionInfo.Data.Instrument, positions, out _))
                    {
                        // there is liquidity so we can cancel the special liquidation flow.
                        sender.SendCommand(new CancelSpecialLiquidationCommand
                        {
                            OperationId = e.OperationId,
                            Reason = "Liquidity is enough to close positions within regular flow"
                        }, _cqrsContextNamesSettings.TradingEngine);
                        return;
                    }
                }

                if (PriceRequestRetryRequired(executionInfo.Data.RequestedFromCorporateActions))
                {
                    var isDiscontinued = await FailIfInstrumentDiscontinued(executionInfo, sender);
                    if (isDiscontinued) return;
                    
                    var pauseAcknowledged = await _rfqPauseService.AcknowledgeAsync(executionInfo.Id);
                    if (pauseAcknowledged) return;
                    
                    if (executionInfo.Data.SwitchState(executionInfo.Data.State,
                            SpecialLiquidationOperationState.PriceRequested))
                    {
                        await InternalRetryPriceRequest(e.CreationTime, sender, executionInfo,
                            _marginTradingSettings.SpecialLiquidation.PriceRequestRetryTimeout.Value);
                        
                        return;
                    }
                }
            }

            if (executionInfo.Data.SwitchState(executionInfo.Data.State,//from any state
                SpecialLiquidationOperationState.Failed))
            {
                if (!string.IsNullOrEmpty(executionInfo.Data.CausationOperationId))
                {
                    sender.SendCommand(new ResumeLiquidationInternalCommand
                    {
                        OperationId = executionInfo.Data.CausationOperationId,
                        CreationTime = _dateService.Now(),
                        Comment = $"Resume after special liquidation {executionInfo.Id} failed. Reason: {e.Reason}",
                        IsCausedBySpecialLiquidation = true,
                        CausationOperationId = executionInfo.Id
                    }, _cqrsContextNamesSettings.TradingEngine);
                }
                
                _chaosKitty.Meow(e.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }
        
        [UsedImplicitly]
        private async Task Handle(SpecialLiquidationCancelledEvent e, ICommandSender sender)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: OperationName,
                id: e.OperationId);
            
            if (executionInfo?.Data == null)
                return;

            if (executionInfo.Data.SwitchState(executionInfo.Data.State,//from any state
                SpecialLiquidationOperationState.Cancelled))
            {
                if (e.ClosePositions)
                {
                    sender.SendCommand(new ClosePositionsRegularFlowCommand
                    {
                        OperationId = e.OperationId
                    }, _cqrsContextNamesSettings.TradingEngine);
                }
                else
                {
                    executionInfo.Data.CancelClosing(id => _ordersCache.Positions.GetPositionById(id),
                        _dateService.Now);
                }

                _chaosKitty.Meow(e.OperationId);
                
                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }
        
        [UsedImplicitly]
        private async Task Handle(ResumePausedSpecialLiquidationFailedEvent e, ICommandSender sender)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: OperationName,
                id: e.OperationId);
            
            if (executionInfo?.Data == null)
                return;

            await _log.WriteWarningAsync(nameof(SpecialLiquidationSaga),
                nameof(ResumePausedSpecialLiquidationFailedEvent), $"Pause cancellation failed. Reason: {e.Reason.ToString()}");
        }
        
        [UsedImplicitly]
        private async Task Handle(ResumePausedSpecialLiquidationSucceededEvent e, ICommandSender sender)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: OperationName,
                id: e.OperationId);
            
            if (executionInfo?.Data == null)
                return;
            
            if (PriceRequestRetryRequired(executionInfo.Data.RequestedFromCorporateActions))
            {
                var isDiscontinued = await FailIfInstrumentDiscontinued(executionInfo, sender);
                if (isDiscontinued) return;

                if (executionInfo.Data.SwitchState(executionInfo.Data.State,
                        SpecialLiquidationOperationState.PriceRequested))
                {
                    await InternalRetryPriceRequest(e.CreationTime, sender, executionInfo,
                        _marginTradingSettings.SpecialLiquidation.PriceRequestRetryTimeout.Value);

                    return;
                }
            }

            if (executionInfo.Data.SwitchState(executionInfo.Data.State,
                    SpecialLiquidationOperationState.OnTheWayToFail))
            {
                sender.SendCommand(new FailSpecialLiquidationInternalCommand
                {
                    OperationId = e.OperationId,
                    CreationTime = _dateService.Now(),
                    Reason = $"Pause cancellation succeeded but then price request was not initiated for operation id [{e.OperationId} and name [{OperationName}]]"
                }, _cqrsContextNamesSettings.TradingEngine);
                
                _chaosKitty.Meow(e.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        private decimal GetActualNetPositionCloseVolume(ICollection<string> positionIds, string accountId)
        {
            var netPositionVolume = _ordersCache.GetPositions()
                .Where(x => positionIds.Contains(x.Id)
                            && (string.IsNullOrEmpty(accountId) || x.AccountId == accountId))
                .Sum(x => x.Volume);

            return -netPositionVolume;
        }

        private void RequestPrice(ICommandSender sender, IOperationExecutionInfo<SpecialLiquidationOperationData> 
            executionInfo)
        {
            //hack, requested by the bank
            var positionsVolume = executionInfo.Data.Volume != 0 ? executionInfo.Data.Volume : 1;
            
            var command = new GetPriceForSpecialLiquidationCommand
            {
                OperationId = executionInfo.Id,
                CreationTime = _dateService.Now(),
                Instrument = executionInfo.Data.Instrument,
                Volume = positionsVolume,
                RequestNumber = executionInfo.Data.RequestNumber,
                RequestedFromCorporateActions = executionInfo.Data.RequestedFromCorporateActions
            };
            
            if (_marginTradingSettings.ExchangeConnector == ExchangeConnectorType.RealExchangeConnector)
            {
                //send it to the Gavel
                sender.SendCommand(command, _cqrsContextNamesSettings.Gavel);
            }
            else
            {
                _specialLiquidationService.SavePriceRequestForSpecialLiquidation(command);
            }
            
            //special command is sent instantly for timeout control.. it is retried until timeout occurs
            sender.SendCommand(new GetPriceForSpecialLiquidationTimeoutInternalCommand
            {
                OperationId = executionInfo.Id,
                CreationTime = _dateService.Now(),
                TimeoutSeconds = _marginTradingSettings.SpecialLiquidation.PriceRequestTimeoutSec,
                RequestNumber = executionInfo.Data.RequestNumber
            }, _cqrsContextNamesSettings.TradingEngine);
        }

        private bool PriceRequestRetryRequired(bool requestedFromCorporateActions) =>
            _marginTradingSettings.SpecialLiquidation.PriceRequestRetryTimeout.HasValue &&
            (!requestedFromCorporateActions ||
             _marginTradingSettings.SpecialLiquidation.RetryPriceRequestForCorporateActions);
        
        private async Task InternalRetryPriceRequest(DateTime eventCreationTime, 
            ICommandSender sender,
            IOperationExecutionInfo<SpecialLiquidationOperationData> executionInfo,
            TimeSpan retryTimeout)
        {
            // fix the intention to make another price request to not let the parallel 
            // ongoing GetPriceForSpecialLiquidationTimeoutInternalCommand execution
            // break (fail) the flow
            executionInfo.Data.NextRequestNumber();
            await _operationExecutionInfoRepository.Save(executionInfo);
            
            var shouldRetryAfter = eventCreationTime.Add(retryTimeout);

            var timeLeftBeforeRetry = shouldRetryAfter - _dateService.Now();

            if (timeLeftBeforeRetry > TimeSpan.Zero)
            {
                await Task.Delay(timeLeftBeforeRetry);
            }

            RequestPrice(sender, executionInfo);
        }

        private async Task<bool> FailIfInstrumentDiscontinued(IOperationExecutionInfo<SpecialLiquidationOperationData> executionInfo, ICommandSender sender)
        {
            var isDiscontinued = _assetPairsCache.GetAssetPairById(executionInfo.Data.Instrument).IsDiscontinued;
            
            if (isDiscontinued)
            {
                if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.PriceRequested,
                        SpecialLiquidationOperationState.OnTheWayToFail))
                {
                    sender.SendCommand(new FailSpecialLiquidationInternalCommand
                    {
                        OperationId = executionInfo.Id,
                        CreationTime = _dateService.Now(),
                        Reason = "Instrument discontinuation",
                            
                    }, _cqrsContextNamesSettings.TradingEngine);
                
                    await _operationExecutionInfoRepository.Save(executionInfo);
                }

                return true;
            }

            return false;
        }
    }
}