// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Rfq;
using MarginTrading.Backend.Services.Notifications;

namespace MarginTrading.Backend.Services.Services
{
    [UsedImplicitly]
    public class RfqExecutionPauseRepositoryDecorator : IOperationExecutionPauseRepository
    {
        private readonly IOperationExecutionPauseRepository _decoratee;
        private readonly IRabbitMqNotifyService _notifyService;
        private readonly ILog _log;

        public RfqExecutionPauseRepositoryDecorator(IOperationExecutionPauseRepository decoratee, ILog log, IRabbitMqNotifyService notifyService)
        {
            _decoratee = decoratee;
            _notifyService = notifyService;
            _log = log;
        }

        public async Task AddAsync(Pause pause)
        {
            await _decoratee.AddAsync(pause);
            
            await _log.WriteInfoAsync(nameof(RfqExecutionPauseRepositoryDecorator),
                nameof(AddAsync),
                pause.ToJson(),
                $"New RFQ pause has been added therefore {nameof(RfqChangedEvent)} is about to be published");

            await _notifyService.RfqChanged(new RfqChangedEvent());
        }

        public Task<IEnumerable<Pause>> FindAsync(string operationId, string operationName, Func<Pause, bool> filter = null)
        {
            return _decoratee.FindAsync(operationId, operationName, filter);
        }

        public async Task<bool> UpdateAsync(long oid,
            DateTime? effectiveSince,
            PauseState state,
            DateTime? cancelledAt,
            DateTime? cancellationEffectiveSince,
            Initiator? cancellationInitiator,
            PauseCancellationSource? cancellationSource)
        {
            var updated = await _decoratee.UpdateAsync(oid,
                effectiveSince,
                state,
                cancelledAt,
                cancellationEffectiveSince,
                cancellationInitiator,
                cancellationSource);

            if (updated)
            {
                await _log.WriteInfoAsync(nameof(RfqExecutionPauseRepositoryDecorator),
                    nameof(UpdateAsync),
                    new { Oid = oid }.ToJson(),
                    $"RFQ pause has been updated therefore {nameof(RfqChangedEvent)} is about to be published");

                await _notifyService.RfqChanged(new RfqChangedEvent());
            }

            return updated;
        }
    }
}