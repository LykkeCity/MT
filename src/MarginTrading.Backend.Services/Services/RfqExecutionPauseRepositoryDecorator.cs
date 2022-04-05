// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Snow.Common;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Rfq;
using MarginTrading.Backend.Services.Extensions;
using MarginTrading.Backend.Services.Notifications;

namespace MarginTrading.Backend.Services.Services
{
    [UsedImplicitly]
    public class RfqExecutionPauseRepositoryDecorator : IOperationExecutionPauseRepository
    {
        private readonly IOperationExecutionPauseRepository _decoratee;
        private readonly IOperationExecutionInfoRepository _executionInfoRepository;
        private readonly IRabbitMqNotifyService _notifyService;
        private readonly ILog _log;

        public RfqExecutionPauseRepositoryDecorator(IOperationExecutionPauseRepository decoratee,
            IRabbitMqNotifyService notifyService,
            IOperationExecutionInfoRepository executionInfoRepository,
            ILog log)
        {
            _decoratee = decoratee;
            _notifyService = notifyService;
            _executionInfoRepository = executionInfoRepository;
            _log = log;
        }

        public async Task AddAsync(Pause pause)
        {
            await _decoratee.AddAsync(pause);
            
            await _log.WriteInfoAsync(nameof(RfqExecutionPauseRepositoryDecorator),
                nameof(AddAsync),
                pause.ToJson(),
                $"New RFQ pause has been added therefore {nameof(RfqEvent)} is about to be published");

            var rfq = await GetRfqByIdAsync(pause.OperationId);
            
            await _notifyService.Rfq(rfq.ToEventContract(RfqTypeContract.Update));
        }

        public Task<IEnumerable<Pause>> FindAsync(string operationId, string operationName, Func<Pause, bool> filter = null)
        {
            return _decoratee.FindAsync(operationId, operationName, filter);
        }

        public Task<Pause> FindAsync(long oid)
        {
            return _decoratee.FindAsync(oid);
        }

        public async Task<bool> UpdateAsync(long oid,
            DateTime? effectiveSince,
            PauseState state,
            DateTime? cancelledAt,
            DateTime? cancellationEffectiveSince,
            Initiator cancellationInitiator,
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
                    $"RFQ pause has been updated therefore {nameof(RfqEvent)} is about to be published");

                var pause = await _decoratee.FindAsync(oid);

                if (pause != null)
                {
                    var rfq = await GetRfqByIdAsync(pause.OperationId);

                    await _notifyService.Rfq(rfq.ToEventContract(RfqTypeContract.Update));
                }
            }

            return updated;
        }
        
        private async Task<OperationExecutionInfoWithPause<SpecialLiquidationOperationData>> GetRfqByIdAsync(string id)
        {
            return (await _executionInfoRepository
                    .GetRfqAsync(0, 1, id))
                .Contents
                .Single();
        }
    }
}