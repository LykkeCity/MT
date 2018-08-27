using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation
{
    [UsedImplicitly]
    public class SpecialLiquidationCommandsHandler
    {
        private readonly IDateService _dateService;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;

        private const string OperationName = "SpecialLiquidation";
        
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
                operationName: OperationName,
                operationId: command.OperationId,
                factory: () => new OperationExecutionInfo<OperationData>(
                    operationName: OperationName,
                    id: command.OperationId,
                    lastModified: _dateService.Now(),
                    data: new OperationData
                    {
                        State = OperationState.Initiated,
                    }
                ));

            if (executionInfo.Data.SwitchState(OperationState.Initiated, OperationState.Started))
            {
                

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }
    }
}