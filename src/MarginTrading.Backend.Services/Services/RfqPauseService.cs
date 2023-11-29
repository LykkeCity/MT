// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Snow.Common;
using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Rfq;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Services
{
    public class RfqPauseService : IRfqPauseService
    {
        private readonly IOperationExecutionPauseRepository _pauseRepository;
        private readonly IOperationExecutionInfoRepository _executionInfoRepository;
        private readonly IDateService _dateService;
        private readonly ICqrsSender _cqrsSender;
        private readonly ILog _log;

        public static readonly IEnumerable<SpecialLiquidationOperationState> AllowedOperationStatesToPauseIn = new[]
        {
            SpecialLiquidationOperationState.Initiated,
            SpecialLiquidationOperationState.Started,
            SpecialLiquidationOperationState.PriceRequested,
            SpecialLiquidationOperationState.PriceReceived
        };

        public static readonly Func<Pause, bool> ActivePredicate = p => p.State == PauseState.Active;
        
        public static readonly Func<Pause, bool> PendingPredicate = p => p.State == PauseState.Pending;

        public static readonly Func<Pause, bool> NotCancelledPredicate = p => p.State != PauseState.Cancelled;
        
        public static readonly Func<Pause, bool> PendingCancellationPredicate = p => p.State == PauseState.PendingCancellation;

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _lock =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        public RfqPauseService(IOperationExecutionPauseRepository pauseRepository,
            IOperationExecutionInfoRepository executionInfoRepository,
            ILog log,
            IDateService dateService,
            ICqrsSender cqrsSender)
        {
            _pauseRepository = pauseRepository;
            _executionInfoRepository = executionInfoRepository;
            _log = log;
            _dateService = dateService;
            _cqrsSender = cqrsSender;
        }

        public async Task<RfqPauseErrorCode> AddAsync(string operationId, PauseSource source, Initiator initiator)
        {
            if (string.IsNullOrEmpty(operationId))
                throw new ArgumentNullException(nameof(operationId));
            
            var locker = _lock.GetOrAdd(operationId, new SemaphoreSlim(1, 1));

            await locker.WaitAsync();

            try
            {
                var existingPause = (await _pauseRepository.FindAsync(
                        operationId,
                        SpecialLiquidationSaga.Name,
                        NotCancelledPredicate))
                    .SingleOrDefault();

                if (existingPause != null)
                {
                    await _log.WriteWarningAsync(nameof(RfqPauseService), nameof(AddAsync), $"There is already pause with state [{existingPause.State}] for operation with id [{operationId}]");
                    return RfqPauseErrorCode.AlreadyExists;
                }
            
                var executionInfo = await _executionInfoRepository
                    .GetAsync<SpecialLiquidationOperationData>(SpecialLiquidationSaga.Name, operationId);
                
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
                    SpecialLiquidationSaga.Name,
                    source,
                    initiator,
                    _dateService.Now());
                
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
                    SpecialLiquidationSaga.Name,
                    NotCancelledPredicate))
                .SingleOrDefault();
        }

        public async Task<bool> AcknowledgeAsync(string operationId)
        {
            var locker = _lock.GetOrAdd(operationId, new SemaphoreSlim(1, 1));

            await locker.WaitAsync();

            try
            {
                var activePause = (await _pauseRepository.FindAsync(
                        operationId,
                        SpecialLiquidationSaga.Name,
                        ActivePredicate))
                    .SingleOrDefault();

                if (activePause != null)
                {
                    await _log.WriteInfoAsync(nameof(RfqPauseService), nameof(AcknowledgeAsync), null,
                        $"The pause for operation id [{operationId}] and name [{SpecialLiquidationSaga.Name}] is effective since [{activePause.EffectiveSince}]");

                    return true;
                }

                var pendingPause = (await _pauseRepository.FindAsync(
                        operationId,
                        SpecialLiquidationSaga.Name,
                        PendingPredicate))
                    .SingleOrDefault();

                if (pendingPause != null)
                {
                    if (pendingPause.Oid == null)
                        throw new InvalidOperationException("Pause oid is required to update");
                    
                    var updated = await _pauseRepository.UpdateAsync(
                        pendingPause.Oid.Value, 
                        _dateService.Now(), 
                        PauseState.Active, 
                        null,
                        null, 
                        null, 
                        null);

                    if (!updated)
                    {
                        await _log.WriteWarningAsync(nameof(RfqPauseService), nameof(AcknowledgeAsync), null,
                            $"Couldn't activate pending pause for operation id [{operationId}] and name [{SpecialLiquidationSaga.Name}]");

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

        public async Task StopPendingAsync(string operationId, PauseCancellationSource source, Initiator initiator)
        {
            var locker = _lock.GetOrAdd(operationId, new SemaphoreSlim(1, 1));

            await locker.WaitAsync();

            try
            {
                var pendingPause = (await _pauseRepository.FindAsync(
                        operationId,
                        SpecialLiquidationSaga.Name,
                        PendingPredicate))
                    .SingleOrDefault();

                if (pendingPause != null)
                {
                    if (pendingPause.Oid == null)
                        throw new InvalidOperationException("Pause oid is required to update");
                    
                    var updated = await _pauseRepository.UpdateAsync(
                        pendingPause.Oid.Value,
                        pendingPause.EffectiveSince,
                        PauseState.Cancelled,
                        _dateService.Now(),
                        _dateService.Now(),
                        initiator,
                        source);
                    
                    if (!updated)
                    {
                        await _log.WriteWarningAsync(nameof(RfqPauseService), nameof(StopPendingAsync), null,
                            $"Couldn't stop pending pause for operation id [{operationId}] and name [{SpecialLiquidationSaga.Name}]");
                    }
                }

                // todo: add audit log
            }
            finally
            {
                locker.Release();
            }
        }
        
        public async Task<bool> AcknowledgeCancellationAsync(string operationId)
        {
            var locker = _lock.GetOrAdd(operationId, new SemaphoreSlim(1, 1));

            await locker.WaitAsync();

            try
            {
                var pendingCancellationPause = (await _pauseRepository.FindAsync(
                        operationId,
                        SpecialLiquidationSaga.Name,
                        PendingCancellationPredicate))
                    .SingleOrDefault();

                if (pendingCancellationPause != null)
                {
                    if (pendingCancellationPause.Oid == null)
                        throw new InvalidOperationException("Pause oid is required to update");
                    
                    var updated = await _pauseRepository.UpdateAsync(
                        pendingCancellationPause.Oid.Value, 
                        pendingCancellationPause.EffectiveSince, 
                        PauseState.Cancelled,
                        pendingCancellationPause.CancelledAt,
                        _dateService.Now(),
                        pendingCancellationPause.CancellationInitiator,
                        pendingCancellationPause.CancellationSource);
                    
                    if (!updated)
                    {
                        await _log.WriteWarningAsync(nameof(RfqPauseService), nameof(AcknowledgeCancellationAsync), null,
                            $"Couldn't cancel pending cancellation pause for operation id [{operationId}] and name [{SpecialLiquidationSaga.Name}]");

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

        public async Task<RfqResumeErrorCode> ResumeAsync(string operationId, PauseCancellationSource source, Initiator initiator)
        {
            if (string.IsNullOrEmpty(operationId))
                throw new ArgumentNullException(nameof(operationId));
            
            var locker = _lock.GetOrAdd(operationId, new SemaphoreSlim(1, 1));

            await locker.WaitAsync();

            try
            {
                var executionInfo = await _executionInfoRepository
                    .GetAsync<SpecialLiquidationOperationData>(SpecialLiquidationSaga.Name, operationId);
                
                if (executionInfo == null)
                    return RfqResumeErrorCode.NotFound;
                
                var activePause = (await _pauseRepository.FindAsync(
                        operationId,
                        SpecialLiquidationSaga.Name,
                        ActivePredicate))
                    .SingleOrDefault();

                if (activePause == null)
                {
                    await _log.WriteInfoAsync(nameof(RfqPauseService), nameof(ResumeAsync), null,
                        $"The active pause for operation id [{operationId}] and name [{SpecialLiquidationSaga.Name}] was not found");

                    return RfqResumeErrorCode.NotPaused;
                }

                // Manual resume is allowed for manually paused RFQ only
                if (source == PauseCancellationSource.Manual && activePause.Source != PauseSource.Manual)
                {
                    await _log.WriteWarningAsync(nameof(RfqPauseService), nameof(ResumeAsync), null,
                        $"Manual resume is allowed for manually paused RFQ only");

                    return RfqResumeErrorCode.ManualResumeDenied;
                }
                
                if (activePause.Oid == null)
                    throw new InvalidOperationException("Pause oid is required to update");

                var updated = await _pauseRepository.UpdateAsync(
                    activePause.Oid.Value, 
                    activePause.EffectiveSince ?? throw new InvalidOperationException("Activated pause must have an [Effective Since] value"), 
                    PauseState.PendingCancellation,
                    _dateService.Now(),
                    null, 
                    initiator, 
                    source);

                if (updated)
                {
                    _cqrsSender.SendCommandToSelf(new ResumePausedSpecialLiquidationCommand{OperationId = operationId});   
                }
                else
                {
                    await _log.WriteWarningAsync(nameof(RfqPauseService), nameof(ResumeAsync), null,
                        $"Couldn't cancel active pause for operation id [{operationId}] and name [{SpecialLiquidationSaga.Name}] due to database issues");

                    return RfqResumeErrorCode.Persistence;
                }
                
                // todo: add audit log
            }
            finally
            {
                locker.Release();
            }

            return RfqResumeErrorCode.None;
        }
    }
}