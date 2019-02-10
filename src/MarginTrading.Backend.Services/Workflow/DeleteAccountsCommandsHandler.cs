using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Common.Log;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    public class DeleteAccountsCommandsHandler
    {
        private readonly IDateService _dateService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IChaosKitty _chaosKitty;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly ILog _log;
        
        private const string OperationName = "DeleteAccounts";
        
        public DeleteAccountsCommandsHandler(
            IDateService dateService,
            IAccountsCacheService accountsCacheService,
            IChaosKitty chaosKitty,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            ILog log)
        {
            _dateService = dateService;
            _accountsCacheService = accountsCacheService;
            _chaosKitty = chaosKitty;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _log = log;
        }

        /// <summary>
        /// MT Core need to block trades and withdrawals on accounts.
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(BlockAccountsForDeletionCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetOrAddAsync(
                operationName: OperationName,
                operationId: command.OperationId,
                factory: () => new OperationExecutionInfo<DeleteAccountsOperationData>(
                    operationName: OperationName,
                    id: command.OperationId,
                    lastModified: _dateService.Now(),
                    data: new DeleteAccountsOperationData
                    {
                        State = OperationState.Initiated
                    }
                ));

            //todo think how to remove state saving from commands handler here and for Withdrawal
            if (executionInfo.Data.SwitchState(OperationState.Initiated, OperationState.Started))
            {
                var failedAccounts = new Dictionary<string, string>();
                foreach (var accountId in command.AccountIds)
                {
                    MarginTradingAccount account = null;
                    try
                    {
                        account = _accountsCacheService.Get(accountId);
                    }
                    catch (Exception exception)
                    {
                        failedAccounts.Add(accountId, exception.Message);
                        continue;
                    }
                    
                    if (account.AccountFpl.WithdrawalFrozenMarginData.Any())
                    {
                        _log.Error(nameof(BlockAccountsForDeletionCommand), 
                            new Exception("While deleting an account it contained some frozen withdrawal data. Account is deleted."), 
                            account.ToJson());
                    }
            
                    if (account.AccountFpl.UnconfirmedMarginData.Any())
                    {
                        _log.Error(nameof(BlockAccountsForDeletionCommand), 
                            new Exception("While deleting an account it contained some unconfirmed margin data. Account is deleted."), 
                            account.ToJson());
                    }

                    if (account.Balance != 0)
                    {
                        _log.Error(nameof(BlockAccountsForDeletionCommand), 
                            new Exception("While deleting an account it's balance on side of TradingCore was non zero. Account is deleted."), 
                            account.ToJson());
                    }

                    try
                    {
                        await _accountsCacheService.UpdateAccountChanges(accountId, account.TradingConditionId,
                            account.WithdrawTransferLimit, true, true, _dateService.Now());
                    }
                    catch (Exception exception)
                    {
                        failedAccounts.Add(accountId, exception.Message);
                        continue;
                    }
                }

                publisher.PublishEvent(new AccountsBlockedForDeletionEvent(
                    operationId: command.OperationId,
                    eventTimestamp: _dateService.Now(),
                    blockedAccountIds: command.AccountIds.Except(failedAccounts.Keys).ToList(),
                    failedAccountIds: failedAccounts
                )); 
                
                _chaosKitty.Meow($"{nameof(BlockAccountsForDeletionCommand)}: " +
                                 "Save_OperationExecutionInfo: " +
                                 $"{command.OperationId}");

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        /// <summary>
        /// Accounts are marked as deleted on side of Accounts Management =>
        ///     remove successfully deleted accounts from cache on side of MT Core
        ///     & unblock trades and withdrawals for failed
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(MtCoreFinishAccountsDeletionCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<DeleteAccountsOperationData>(
                operationName: OperationName,
                id: command.OperationId
            );

            if (executionInfo == null)
                return;

            if (executionInfo.Data.SwitchState(OperationState.Started, OperationState.Finished))
            {
                foreach (var failedAccountId in command.FailedAccountIds)
                {
                    try
                    {
                        var account = _accountsCacheService.Get(failedAccountId);

                        await _accountsCacheService.UpdateAccountChanges(failedAccountId, account.TradingConditionId,
                            account.WithdrawTransferLimit, false, false, _dateService.Now());
                    }
                    catch (Exception exception)
                    {
                        _log.Error(nameof(MtCoreFinishAccountsDeletionCommand), exception, exception.Message);
                    }
                }

                foreach (var accountId in command.AccountIds)
                {
                    _accountsCacheService.Remove(accountId);
                }
                
                publisher.PublishEvent(new MtCoreDeleteAccountsFinishedEvent(command.OperationId, _dateService.Now()));
                
                _chaosKitty.Meow($"{nameof(MtCoreFinishAccountsDeletionCommand)}: " +
                                 "Save_OperationExecutionInfo: " +
                                 $"{command.OperationId}");
                
                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }
    }
}