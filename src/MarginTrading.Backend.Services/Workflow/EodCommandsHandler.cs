// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using BookKeeper.Client.Workflow.Commands;
using BookKeeper.Client.Workflow.Events;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Workflow;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    [UsedImplicitly]
    public class EodCommandsHandler
    {
        private readonly ISnapshotService _snapshotService;
        private readonly IDateService _dateService;
        private readonly ILog _log;
        private readonly IChaosKitty _chaosKitty;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private const string OperationName = "EOD";

        public EodCommandsHandler(ISnapshotService snapshotService, IDateService dateService, ILog log, 
            IChaosKitty chaosKitty, IOperationExecutionInfoRepository operationExecutionInfoRepository)
        {
            _snapshotService = snapshotService;
            _dateService = dateService;
            _log = log;
            _chaosKitty = chaosKitty;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
        }
        
        [UsedImplicitly]
        private async Task Handle(CreateSnapshotCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetOrAddAsync(
                operationName: OperationName,
                operationId: command.OperationId,
                factory: () => new OperationExecutionInfo<EodOperationData>(
                    operationName: OperationName,
                    id: command.OperationId,
                    lastModified: _dateService.Now(),
                    data: new EodOperationData
                    {
                        State = OperationState.Initiated,
                        TradingDay = command.TradingDay,
                    }
                ));

            if (executionInfo.Data.SwitchState(OperationState.Initiated, OperationState.Finished))
            {
                try
                {
                    await _snapshotService.MakeTradingDataSnapshot(executionInfo.Data.TradingDay, executionInfo.Id);
                    
                    publisher.PublishEvent(new SnapshotCreatedEvent
                    {
                        OperationId = executionInfo.Id,
                        CreationTime = _dateService.Now(),
                    });
                }
                catch (ArgumentException argumentException)
                {
                    _log.WriteWarning(nameof(EodCommandsHandler), nameof(CreateSnapshotCommand),
                        argumentException.Message, argumentException);
                    
                    return;
                }
                catch (Exception exception)
                {
                    _log.WriteError(nameof(EodCommandsHandler), nameof(CreateSnapshotCommand), exception);
                    
                    publisher.PublishEvent(new SnapshotCreationFailedEvent
                    {
                        OperationId = executionInfo.Id,
                        CreationTime = _dateService.Now(),
                        FailReason = exception.Message,
                    });
                    
                    return; // state is not switched => it will work just fine on EOD re-run.
                }

                _chaosKitty.Meow(command.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }
    }
}