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

        public EodCommandsHandler(ISnapshotService snapshotService, IDateService dateService, ILog log)
        {
            _snapshotService = snapshotService;
            _dateService = dateService;
            _log = log;
        }
        
        [UsedImplicitly]
        private async Task Handle(CreateSnapshotCommand command, IEventPublisher publisher)
        {
            //deduplication is inside _snapshotService.MakeTradingDataSnapshot
            try
            {
                await _snapshotService.MakeTradingDataSnapshot(command.TradingDay, command.OperationId);
                
                publisher.PublishEvent(new SnapshotCreatedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                });
            }
            catch (ArgumentException argumentException)
            {
                _log.WriteWarning(nameof(EodCommandsHandler), nameof(CreateSnapshotCommand),
                    argumentException.Message, argumentException);
            }
            catch (Exception exception)
            {
                _log.WriteError(nameof(EodCommandsHandler), nameof(CreateSnapshotCommand), exception);
                
                publisher.PublishEvent(new SnapshotCreationFailedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    FailReason = exception.Message,
                });
            }
        }
    }
}