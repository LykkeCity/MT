using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;
using MarginTrading.Backend.Services.Workflow.Liquidation.Events;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Events;
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

        public LiquidationCommandsHandler(IAccountsCacheService accountsCache,
            IDateService dateService,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            IChaosKitty chaosKitty)
        {
            _accountsCache = accountsCache;
            _dateService = dateService;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _chaosKitty = chaosKitty;
        }

        [UsedImplicitly]
        private async Task Handle(StartLiquidationInternalCommand command, 
            IEventPublisher publisher)
        {
            if (_accountsCache.TryGet(command.AccountId) == null)
            {
                publisher.PublishEvent(new LiquidationFailedInternalEvent
                {
                    OperationId = command.OperationId, 
                    CreationTime = _dateService.Now(),
                    Reason = "Account does not exist"
                });

                return;
            }
            
            //ensure idempotency
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
                        Direction = command.Direction
                    }
                ));
            
            if (executionInfo.Data.SwitchState(LiquidationOperationState.Initiated, LiquidationOperationState.Started))
            {
                if (!_accountsCache.TryStartLiquidation(command.AccountId, command.OperationId,
                    out var currentOperationId))
                {
                    if (currentOperationId != command.OperationId)
                    {
                        publisher.PublishEvent(new LiquidationFailedInternalEvent
                        {
                            OperationId = command.OperationId,
                            CreationTime = _dateService.Now(),
                            Reason =
                                $"Liquidation is already in progress. Initiated by operation : {currentOperationId}"
                        });
                    }

                    return;
                }

                publisher.PublishEvent(new LiquidationStartedInternalEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now()
                });

                _chaosKitty.Meow($"{LiquidationSaga.OperationName}: {command.OperationId}");

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }
    }
}