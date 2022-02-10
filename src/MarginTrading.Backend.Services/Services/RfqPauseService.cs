// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Rfq;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Services
{
    public class RfqPauseService : IRfqPauseService
    {
        private readonly IOperationExecutionPauseRepository _pauseRepository;
        private readonly IOperationExecutionInfoRepository _executionInfoRepository;
        private readonly IDateService _dateService;
        private readonly ILog _log;

        private static readonly IEnumerable<SpecialLiquidationOperationState> AllowedOperationStatesToPauseIn = new[]
        {
            SpecialLiquidationOperationState.PriceRequested,
            SpecialLiquidationOperationState.PriceReceived
        };

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _lock =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        public RfqPauseService(IOperationExecutionPauseRepository pauseRepository,
            IOperationExecutionInfoRepository executionInfoRepository,
            ILog log,
            IDateService dateService)
        {
            _pauseRepository = pauseRepository;
            _executionInfoRepository = executionInfoRepository;
            _log = log;
            _dateService = dateService;
        }

        public async Task<RfqPauseErrorCode> AddAsync(string operationId, Initiator initiator)
        {
            if (string.IsNullOrEmpty(operationId))
                throw new ArgumentNullException(nameof(operationId));
            
            var locker = _lock.GetOrAdd(operationId, new SemaphoreSlim(1, 1));

            await locker.WaitAsync();

            try
            {
                var existingPause = (await _pauseRepository.FindAsync(
                        operationId,
                        SpecialLiquidationSaga.OperationName,
                        o => o.State != PauseState.Cancelled))
                    .SingleOrDefault();

                if (existingPause != null)
                {
                    await _log.WriteWarningAsync(nameof(RfqPauseService), nameof(AddAsync), $"There is already pause with state [{existingPause.State}] for operation with id [{operationId}]");
                    return RfqPauseErrorCode.AlreadyExists;
                }
            
                var executionInfo = await _executionInfoRepository
                    .GetAsync<SpecialLiquidationOperationData>(SpecialLiquidationSaga.OperationName, operationId);
                
                if (executionInfo == null)
                    return RfqPauseErrorCode.NotFound;

                if (!AllowedOperationStatesToPauseIn.Contains(executionInfo.Data.State))
                {
                    await _log.WriteWarningAsync(nameof(RfqPauseService), nameof(AddAsync),
                        $"There was an attempt to pause special liquidation with id {operationId} and state {executionInfo.Data.State}. Pause is possible in [{string.Join(',', AllowedOperationStatesToPauseIn)}] states only");

                    return RfqPauseErrorCode.InvalidOperationState;
                }
                
                var pause = Pause.Create(
                    operationId,
                    SpecialLiquidationSaga.OperationName,
                    _dateService.Now(),
                    null,
                    PauseState.Pending,
                    PauseSource.Manual,
                    initiator,
                    null,
                    null,
                    null,
                    null);
                
                await _pauseRepository.AddAsync(pause);

                // todo: add audit log
            }
            finally
            {
                locker.Release();
            }

            return RfqPauseErrorCode.None;
        }

        public async Task<Pause> GetCurrentAsync(string operationId)
        {
            if (string.IsNullOrEmpty(operationId))
                throw new ArgumentNullException(nameof(operationId));

            return (await _pauseRepository.FindAsync(
                    operationId,
                    SpecialLiquidationSaga.OperationName,
                    o => o.State != PauseState.Cancelled))
                .SingleOrDefault();
        }

        public async Task<bool> AcknowledgeIfPausedAsync(string operationId)
        {
            var locker = _lock.GetOrAdd(operationId, new SemaphoreSlim(1, 1));

            await locker.WaitAsync();

            try
            {
                var activePause = (await _pauseRepository.FindAsync(
                        operationId,
                        SpecialLiquidationSaga.OperationName,
                        o => o.State == PauseState.Active))
                    .SingleOrDefault();

                if (activePause != null)
                {
                    await _log.WriteInfoAsync(nameof(RfqPauseService), nameof(AcknowledgeIfPausedAsync), null,
                        $"The pause for operation id [{operationId}] and name [{SpecialLiquidationSaga.OperationName}] is effective since [{activePause.EffectiveSince}]");

                    return true;
                }

                var pendingPause = (await _pauseRepository.FindAsync(
                        operationId,
                        SpecialLiquidationSaga.OperationName,
                        o => o.State == PauseState.Pending))
                    .SingleOrDefault();

                if (pendingPause != null)
                {
                    var updated = await _pauseRepository.UpdateAsync(
                        pendingPause.Oid ?? throw new InvalidOperationException("Pause oid is required to update"), 
                        _dateService.Now(), 
                        PauseState.Active, 
                        null,
                        null, 
                        null, 
                        null);

                    if (!updated)
                    {
                        await _log.WriteWarningAsync(nameof(RfqPauseService), nameof(AcknowledgeIfPausedAsync), null,
                            $"Couldn't activate pending pause for operation id [{operationId}] and name [{SpecialLiquidationSaga.OperationName}]");

                        return false;
                    }
                    
                    // todo: add audit log

                    return true;
                }
            }
            finally
            {
                locker.Release();
            }
            
            return false;
        }

        public async Task ContinueAsync(string operationId)
        {
            throw new System.NotImplementedException();
        }
    }
}