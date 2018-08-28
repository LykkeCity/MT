using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
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
        private readonly CqrsContextNamesSettings _cqrsContextNamesSettings;
        
        public const string OperationName = "SpecialLiquidation";

        public SpecialLiquidationSaga(
            IDateService dateService,
            IChaosKitty chaosKitty,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            CqrsContextNamesSettings cqrsContextNamesSettings)
        {
            _dateService = dateService;
            _chaosKitty = chaosKitty;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
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
                //todo grab data here
                //todo use timeout for a call, generate SpecialLiquidationFailedEvent on timeout and break
                
                //send it to the Gavel
                sender.SendCommand(new GetPriceForSpecialLiquidationCommand
                {
                    OperationId = e.OperationId,
                    CreationTime = _dateService.Now(),
                    Instrument = "",//TODO fix
                    Volume = default,//TODO fix
                }, _cqrsContextNamesSettings.Gavel);
                
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

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.PriceRequested,
                SpecialLiquidationOperationState.PriceReceived))
            {
                //some logic to peek either to execute order or not
                //TODO i.e. validate that volume didn't changed

                executionInfo.Data.Instrument = e.Instrument;
                executionInfo.Data.Volume = e.Volume;
                executionInfo.Data.Price = e.Price;
                
                //send command to execute order in Gavel
                sender.SendCommand(new ExecuteSpecialLiquidationOrderCommand
                {
                    OperationId = e.OperationId,
                    CreationTime = _dateService.Now(),
                    Instrument = e.Instrument,
                    Volume = e.Volume,
                    Price = e.Price,
                }, _cqrsContextNamesSettings.Gavel);
                
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

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.PriceReceived,
                SpecialLiquidationOperationState.ExternalOrderExecuted))
            {
                sender.SendCommand(new ExecuteSpecialLiquidationOrdersInternalCommand
                {
                    OperationId = e.OperationId,
                    CreationTime = _dateService.Now(),
                    Instrument = executionInfo.Data.Instrument,
                    Volume = executionInfo.Data.Volume,
                    Price = executionInfo.Data.Price,
                }, _cqrsContextNamesSettings.Gavel);
                
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

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.ExternalOrderExecuted,
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

            if (executionInfo.Data.SwitchState(executionInfo.Data.State,//from any state
                SpecialLiquidationOperationState.Failed))
            {
                //do nothing
                
                _chaosKitty.Meow(e.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }
    }
}