// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
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
        private readonly IOrderReader _orderReader;
        private readonly ISpecialLiquidationService _specialLiquidationService;

        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly CqrsContextNamesSettings _cqrsContextNamesSettings;
        
        public const string OperationName = "SpecialLiquidation";

        public SpecialLiquidationSaga(
            IDateService dateService,
            IChaosKitty chaosKitty,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            IOrderReader orderReader,
            ISpecialLiquidationService specialLiquidationService,
            MarginTradingSettings marginTradingSettings,
            CqrsContextNamesSettings cqrsContextNamesSettings)
        {
            _dateService = dateService;
            _chaosKitty = chaosKitty;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _orderReader = orderReader;
            _specialLiquidationService = specialLiquidationService;
            _marginTradingSettings = marginTradingSettings;
            _cqrsContextNamesSettings = cqrsContextNamesSettings;
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
                var positionsVolume = GetNetPositionCloseVolume(executionInfo.Data.PositionIds, executionInfo.Data.AccountId);

                executionInfo.Data.Instrument = e.Instrument;
                executionInfo.Data.Volume = positionsVolume;
                executionInfo.Data.RequestNumber = 1;

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

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.PriceRequested,
                SpecialLiquidationOperationState.PriceReceived))
            {
                //validate that volume didn't changed to peek either to execute order or request the price again
                var currentVolume = GetNetPositionCloseVolume(executionInfo.Data.PositionIds, executionInfo.Data.AccountId);
                if (currentVolume != 0 && currentVolume != executionInfo.Data.Volume)
                {
                    executionInfo.Data.RequestNumber++;
                    executionInfo.Data.Volume = currentVolume;
                    
                    RequestPrice(sender, executionInfo);
                    
                    await _operationExecutionInfoRepository.Save(executionInfo);
                    
                    return;//wait for the new price
                }

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
        }
        
        [UsedImplicitly]
        private async Task Handle(PriceForSpecialLiquidationCalculationFailedEvent e, ICommandSender sender)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: OperationName,
                id: e.OperationId);
            
            if (executionInfo?.Data == null)
                return;

            if (await RetryPriceRequestIfNeeded(e.CreationTime, sender, executionInfo, SpecialLiquidationOperationState.PriceRequested)) 
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

            if (await RetryPriceRequestIfNeeded(e.CreationTime, sender, executionInfo,
                SpecialLiquidationOperationState.ExternalOrderExecuted))
                return;

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

            if (e.CanRetryPriceRequest &&
                await RetryPriceRequestIfNeeded(e.CreationTime, sender, executionInfo, executionInfo.Data.State))
                return;

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
                _chaosKitty.Meow(e.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        private decimal GetNetPositionCloseVolume(ICollection<string> positionIds, string accountId)
        {
            var netPositionVolume = _orderReader.GetPositions()
                .Where(x => positionIds.Contains(x.Id)
                            && (string.IsNullOrEmpty(accountId) || x.AccountId == accountId))
                .Sum(x => x.Volume);

            return -netPositionVolume;
        }
        
        private void RequestPrice(ICommandSender sender, IOperationExecutionInfo<SpecialLiquidationOperationData> 
            executionInfo)
        {
            if (_marginTradingSettings.ExchangeConnector == ExchangeConnectorType.RealExchangeConnector)
            {
                //hack, requested by the bank
                var positionsVolume = executionInfo.Data.Volume != 0 ? executionInfo.Data.Volume : 1;

                //send it to the Gavel
                sender.SendCommand(new GetPriceForSpecialLiquidationCommand
                {
                    OperationId = executionInfo.Id,
                    CreationTime = _dateService.Now(),
                    Instrument = executionInfo.Data.Instrument,
                    Volume = positionsVolume,
                    RequestNumber = executionInfo.Data.RequestNumber,
                    RequestedFromCorporateActions = executionInfo.Data.RequestedFromCorporateActions
                }, _cqrsContextNamesSettings.Gavel);

                //special command is sent instantly for timeout control.. it is retried until timeout occurs
                sender.SendCommand(new GetPriceForSpecialLiquidationTimeoutInternalCommand
                {
                    OperationId = executionInfo.Id,
                    CreationTime = _dateService.Now(),
                    TimeoutSeconds = _marginTradingSettings.SpecialLiquidation.PriceRequestTimeoutSec,
                    RequestNumber = executionInfo.Data.RequestNumber
                }, _cqrsContextNamesSettings.TradingEngine);
            }
            else
            {
                _specialLiquidationService.FakeGetPriceForSpecialLiquidation(executionInfo.Id,
                    executionInfo.Data.Instrument, executionInfo.Data.Volume);
            }
        }
        
        private async Task<bool> RetryPriceRequestIfNeeded(DateTime eventCreationTime, ICommandSender sender,
            IOperationExecutionInfo<SpecialLiquidationOperationData> executionInfo,
            SpecialLiquidationOperationState currentState)
        {
            if (_marginTradingSettings.SpecialLiquidation.PriceRequestRetryTimeout.HasValue
                && (!executionInfo.Data.RequestedFromCorporateActions
                    || _marginTradingSettings.SpecialLiquidation.RetryPriceRequestForCorporateActions)
                && executionInfo.Data.SwitchState(currentState, 
                    SpecialLiquidationOperationState.PriceRequested))
            {
                var now = _dateService.Now();
                var shouldRetryAfter =
                    eventCreationTime.Add(_marginTradingSettings.SpecialLiquidation.PriceRequestRetryTimeout.Value);

                var timeLeftBeforeRetry = shouldRetryAfter - now;

                if (timeLeftBeforeRetry > TimeSpan.Zero)
                {
                    await Task.Delay(timeLeftBeforeRetry);
                }

                executionInfo.Data.RequestNumber++;

                RequestPrice(sender, executionInfo);

                await _operationExecutionInfoRepository.Save(executionInfo);

                return true;
            }

            return false;
        }
    }
}