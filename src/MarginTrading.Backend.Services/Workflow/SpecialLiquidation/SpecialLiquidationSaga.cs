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

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.Started, 
                SpecialLiquidationOperationState.PriceRequested))
            {
                var positionsVolume = GetCurrentVolume(executionInfo.Data.PositionIds);
                
                //special command is sent instantly for timeout control.. it is retried until timeout occurs
                sender.SendCommand(new GetPriceForSpecialLiquidationTimeoutInternalCommand
                {
                    OperationId = e.OperationId,
                    CreationTime = _dateService.Now(),
                    TimeoutSeconds = _marginTradingSettings.SpecialLiquidation.PriceRequestTimeoutSec,
                }, _cqrsContextNamesSettings.TradingEngine);

                if (_marginTradingSettings.ExchangeConnector == ExchangeConnectorType.RealExchangeConnector)
                {
                    //send it to the Gavel
                    sender.SendCommand(new GetPriceForSpecialLiquidationCommand
                    {
                        OperationId = e.OperationId,
                        CreationTime = _dateService.Now(),
                        Instrument = e.Instrument,
                        Volume = positionsVolume,
                        RequestNumber = 1,
                    }, _cqrsContextNamesSettings.Gavel);
                }
                else
                {
                    _specialLiquidationService.FakeGetPriceForSpecialLiquidation(e.OperationId, e.Instrument,
                        positionsVolume);
                }

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
            
            if (executionInfo == null)
                return;

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.PriceRequested,
                SpecialLiquidationOperationState.PriceReceived))
            {
                //validate that volume didn't changed to peek either to execute order or request the price again
                var currentVolume = GetCurrentVolume(executionInfo.Data.PositionIds);
                if (currentVolume != e.Volume)
                {
                    sender.SendCommand(new GetPriceForSpecialLiquidationCommand
                    {
                        OperationId = e.OperationId,
                        CreationTime = _dateService.Now(),
                        Instrument = e.Instrument,
                        Volume = currentVolume,
                        RequestNumber = e.RequestNumber++,
                    }, _cqrsContextNamesSettings.Gavel);
                    
                    return;//wait for the new price
                }

                executionInfo.Data.Instrument = e.Instrument;
                executionInfo.Data.Volume = e.Volume;
                executionInfo.Data.Price = e.Price;

                //execute order in Gavel by API or use fake
                if (_marginTradingSettings.ExchangeConnector == ExchangeConnectorType.RealExchangeConnector)
                {
                    sender.SendCommand(new ExecuteSpecialLiquidationOrderCommand
                    {
                        OperationId = e.OperationId,
                        CreationTime = _dateService.Now(),
                        Instrument = e.Instrument,
                        Volume = e.Volume,
                        Price = e.Price,
                    }, _cqrsContextNamesSettings.Gavel);
                }
                else
                {
                    _specialLiquidationService.FakeExecuteSpecialLiquidationOrder(e.OperationId, e.Instrument, e.Volume, e.Price);
                }

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
            
            if (executionInfo == null)
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

            if (executionInfo == null)
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
                    Price = executionInfo.Data.Price,
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
            
            if (executionInfo == null)
                return;

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.PriceReceived,
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

            if (executionInfo == null)
                return;
            
            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.InternalOrdersExecuted,
                SpecialLiquidationOperationState.Finished))
            {
                //do nothing
                
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
            
            if (executionInfo == null)
                return;

            if (executionInfo.Data.SwitchState(executionInfo.Data.State,//from any state
                SpecialLiquidationOperationState.Failed))
            {
                //do nothing
                
                _chaosKitty.Meow(e.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        private decimal GetCurrentVolume(List<string> positionIds)
        {
            return _orderReader.GetPositions().Where(x => positionIds.Contains(x.Id)).Sum(x => x.Volume);
        }
    }
}