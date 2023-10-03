// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Common.Services;
using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Workflow
{
    public class WithdrawalCommandsHandler
    {
        private readonly IDateService _dateService;
        private readonly IAccountsProvider _accountsProvider;
        private readonly IChaosKitty _chaosKitty;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly ILogger<WithdrawalCommandsHandler> _logger;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Lock =
            new ConcurrentDictionary<string, SemaphoreSlim>();
        
        private const string OperationName = "FreezeAmountForWithdrawal";

        public WithdrawalCommandsHandler(
            IDateService dateService,
            IChaosKitty chaosKitty,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            ILogger<WithdrawalCommandsHandler> logger,
            IAccountsProvider accountsProvider)
        {
            _dateService = dateService;
            _chaosKitty = chaosKitty;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _logger = logger;
            _accountsProvider = accountsProvider;
        }

        /// <summary>
        /// Freeze the the amount in the margin.
        /// </summary>
        [UsedImplicitly]
        public async Task Handle(FreezeAmountForWithdrawalCommand command, IEventPublisher publisher)
        {
            var (executionInfo, _) = await _operationExecutionInfoRepository.GetOrAddAsync(
                operationName: OperationName,
                operationId: command.OperationId,
                factory: () => new OperationExecutionInfo<WithdrawalFreezeOperationData>(
                    operationName: OperationName,
                    id: command.OperationId,
                    lastModified: _dateService.Now(),
                    data: new WithdrawalFreezeOperationData
                    {
                        State = OperationState.Initiated,
                        AccountId = command.AccountId,
                        Amount = command.Amount,
                    }
                ));

            var account = _accountsProvider.GetAccountById(command.AccountId);
            if (account == null)
            {
                _logger.LogWarning("Freezing the amount for withdrawal has failed. Reason: Failed to get account data. " +
                                   "Details: (OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount})",
                    command.OperationId, command.AccountId, command.Amount);

                publisher.PublishEvent(new AmountForWithdrawalFreezeFailedEvent(command.OperationId, _dateService.Now(),
                    command.AccountId, command.Amount, $"Failed to get account {command.AccountId}"));
                return;
            }

            if (executionInfo.Data.SwitchState(OperationState.Initiated, OperationState.Started))
            {
                var succeeded = await WithAccountLock(command.AccountId, async () =>
                {
                    var frozen = false;
                    
                    var disposableCapital = await _accountsProvider.GetDisposableCapital(command.AccountId);
                    if (disposableCapital.HasValue && account.CanWithdraw(disposableCapital.Value, command.Amount))
                    {
                        frozen = account.TryFreezeWithdrawalMargin(command.OperationId, command.Amount);
                    }

                    if (frozen)
                    {
                        publisher.PublishEvent(new AmountForWithdrawalFrozenEvent(command.OperationId,
                            _dateService.Now(), command.AccountId, command.Amount, command.Reason));
                    }
                    else
                    {
                        publisher.PublishEvent(new AmountForWithdrawalFreezeFailedEvent(command.OperationId,
                            _dateService.Now(),
                            command.AccountId, 
                            command.Amount,
                            $"Couldn't freeze withdrawal margin for account {command.AccountId}"));
                    }

                    return frozen;
                });

                if (succeeded)
                {
                    _logger.LogInformation("The amount for withdrawal has been frozen. " +
                        "Details: (OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount})",
                        command.OperationId, command.AccountId, command.Amount);
                }
                else
                {
                    _logger.LogWarning("Freezing the amount for withdrawal has failed. " +
                                       "Details: (Amount: {Amount}, AccountId: {AccountId}, OperationId: {OperationId}, Reason: {Reason})",
                        command.Amount, 
                        command.AccountId, 
                        command.OperationId,
                        $"Couldn't freeze withdrawal margin for account {command.AccountId}");
                }
                
                _chaosKitty.Meow(command.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        /// <summary>
        /// Withdrawal failed => margin must be unfrozen.
        /// </summary>
        /// <remarks>Errors are not handled => if error occurs event will be retried</remarks>
        [UsedImplicitly]
        private async Task Handle(UnfreezeMarginOnFailWithdrawalCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<WithdrawalFreezeOperationData>(
                operationName: OperationName,
                id: command.OperationId
            );

            if (executionInfo == null)
                return;
            
            var account = _accountsProvider.GetAccountById(executionInfo.Data.AccountId);
            if (account == null)
            {
                throw new InvalidOperationException(
                    $"Failed to get account {executionInfo.Data.AccountId} for unfreezing margin on fail withdrawal");
            }

            if (executionInfo.Data.SwitchState(OperationState.Started, OperationState.Finished))
            {
                account.TryUnfreezeWithdrawalMargin(command.OperationId);

                publisher.PublishEvent(new UnfreezeMarginOnFailSucceededWithdrawalEvent(command.OperationId,
                    _dateService.Now(), executionInfo.Data.AccountId, executionInfo.Data.Amount));

                _chaosKitty.Meow(command.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        [ItemCanBeNull]
        private async Task<T> WithAccountLock<T>(string accountId, Func<Task<T>> action)
        {
            var locker = Lock.GetOrAdd(accountId, new SemaphoreSlim(1, 1));
            await locker.WaitAsync();
            try
            {
                var result = await action();
                return result;
            }
            finally
            {
                locker.Release();
            }
        }
    }
}