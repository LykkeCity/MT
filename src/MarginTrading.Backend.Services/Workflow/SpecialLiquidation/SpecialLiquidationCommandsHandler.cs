using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Events;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation
{
    [UsedImplicitly]
    public class SpecialLiquidationCommandsHandler
    {
        private readonly IDateService _dateService;
        private readonly IChaosKitty _chaosKitty;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        
        public SpecialLiquidationCommandsHandler(
            IDateService dateService,
            IOperationExecutionInfoRepository operationExecutionInfoRepository)
        {
            _dateService = dateService;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
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
    }
}