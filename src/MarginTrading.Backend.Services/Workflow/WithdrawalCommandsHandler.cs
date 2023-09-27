// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
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
        private const string OperationName = "FreezeAmountForWithdrawal";

        private static readonly ConcurrentDictionary<string, object> LockObjects =
            new ConcurrentDictionary<string, object>();

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
                // freezeSucceeded is used to minimize the scope under lock
                var freezeSucceeded = false;

                lock (GetLockObject(command.AccountId))
                {
                    if (account.CanWithdraw(command.Amount))
                    {
                        freezeSucceeded = account.TryFreezeWithdrawalMargin(command.OperationId, command.Amount);
                    }
                }

                if (freezeSucceeded)
                {
                    _chaosKitty.Meow(command.OperationId);

                    _logger.LogInformation("The amount for withdrawal has been frozen. " +
                        "Details: (OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount})",
                        command.OperationId, command.AccountId, command.Amount);

                    publisher.PublishEvent(new AmountForWithdrawalFrozenEvent(command.OperationId,
                        _dateService.Now(),
                        command.AccountId, command.Amount, command.Reason));
                }
                else
                {
                    var reasonStr = $"Couldn't freeze withdrawal margin for account {command.AccountId}";

                    _logger.LogWarning("Freezing the amount for withdrawal has failed. " +
                        "Details: (Amount: {Amount}, AccountId: {AccountId}, OperationId: {OperationId}, Reason: {Reason})",
                        command.Amount, command.AccountId, command.OperationId, reasonStr);

                    publisher.PublishEvent(new AmountForWithdrawalFreezeFailedEvent(command.OperationId,
                        _dateService.Now(),
                        command.AccountId, command.Amount, reasonStr));
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

        private object GetLockObject(string accountId)
        {
            return LockObjects.GetOrAdd(accountId, new object());
        }
    }
}