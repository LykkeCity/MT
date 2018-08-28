using System;
using System.Threading.Tasks;
using Common;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Events;
using MarginTrading.Common.Services;
using WampSharp.Logging;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation
{
    [UsedImplicitly]
    public class SpecialLiquidationCommandsHandler
    {
        private readonly IDateService _dateService;
        private readonly IChaosKitty _chaosKitty;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly ILog _log;
        
        public SpecialLiquidationCommandsHandler(
            IDateService dateService,
            IChaosKitty chaosKitty,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            ILog log)
        {
            _dateService = dateService;
            _chaosKitty = chaosKitty;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _log = log;
        }

        [UsedImplicitly]
        private async Task Handle(StartSpecialLiquidationInternalCommand command, IEventPublisher publisher)
        {
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
                    }
                ));

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.Initiated, SpecialLiquidationOperationState.Started))
            {
                publisher.PublishEvent(new SpecialLiquidationStartedInternalEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                });
                
                _chaosKitty.Meow(command.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        [UsedImplicitly]
        private async Task Handle(ExecuteSpecialLiquidationOrdersInternalCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: SpecialLiquidationSaga.OperationName,
                id: command.OperationId);

            if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.ExternalOrderExecuted,
                SpecialLiquidationOperationState.InternalOrdersExecuted))
            {
                try
                {
                    //close positions with the quote from gavel
                    //todo make positions close
                    
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