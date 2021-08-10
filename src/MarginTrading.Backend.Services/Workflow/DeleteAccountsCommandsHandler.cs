// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
using MarginTrading.Backend.Contracts.Activities;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    public class DeleteAccountsCommandsHandler
    {
        private readonly IOrderReader _orderReader;
        private readonly IDateService _dateService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly ITradingEngine _tradingEngine;
        private readonly IChaosKitty _chaosKitty;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly ILog _log;
        
        private const string OperationName = "DeleteAccounts";
        
        public DeleteAccountsCommandsHandler(
            IOrderReader orderReader,
            IDateService dateService,
            IAccountsCacheService accountsCacheService,
            ITradingEngine tradingEngine,
            IChaosKitty chaosKitty,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            ILog log)
        {
            _orderReader = orderReader;
            _dateService = dateService;
            _accountsCacheService = accountsCacheService;
            _tradingEngine = tradingEngine;
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
                    
                    var positionsCount = _orderReader.GetPositions().Count(x => x.AccountId == accountId);                    
                    if (positionsCount != 0)
                    {
                        failedAccounts.Add(accountId, $"Account contain {positionsCount} open positions which must be closed before account deletion.");
                        continue;
                    }

                    var orders = _orderReader.GetPending().Where(x => x.AccountId == accountId).ToList();
                    if (orders.Any())
                    {
                        var (failedToCloseOrderId, failReason) = ((string)null, (string)null);
                        foreach (var order in orders)
                        {
                            try
                            {
                                _tradingEngine.CancelPendingOrder(order.Id, order.AdditionalInfo,command.OperationId, 
                                $"{nameof(DeleteAccountsCommandsHandler)}: force close all orders.",
                                OrderCancellationReason.AccountInactivated); 
                            }
                            catch (Exception exception)
                            {
                                failedToCloseOrderId = order.Id;
                                failReason = exception.Message;
                                break;
                            }
                        }

                        if (failedToCloseOrderId != null)
                        {
                            failedAccounts.Add(accountId, $"Failed to close order [{failedToCloseOrderId}]: {failReason}.");
                            continue;
                        }
                    }
                    
                    if (account.AccountFpl.WithdrawalFrozenMarginData.Any())
                    {
                        await _log.WriteErrorAsync(nameof(DeleteAccountsCommandsHandler), 
                            nameof(BlockAccountsForDeletionCommand), account.ToJson(), 
                            new Exception("While deleting an account it contained some frozen withdrawal data. Account is deleted."));
                    }
            
                    if (account.AccountFpl.UnconfirmedMarginData.Any())
                    {
                        await _log.WriteErrorAsync(nameof(DeleteAccountsCommandsHandler), 
                            nameof(BlockAccountsForDeletionCommand), account.ToJson(), 
                            new Exception("While deleting an account it contained some unconfirmed margin data. Account is deleted."));
                    }

                    if (account.Balance != 0)
                    {
                        await _log.WriteErrorAsync(nameof(DeleteAccountsCommandsHandler),
                            nameof(BlockAccountsForDeletionCommand), account.ToJson(),
                            new Exception("While deleting an account it's balance on side of TradingCore was non zero. Account is deleted."));
                    }

                    if (!await UpdateAccount(account, true, 
                        r => failedAccounts.Add(accountId, r), command.Timestamp))
                    {
                        continue;
                    }
                }

                publisher.PublishEvent(new AccountsBlockedForDeletionEvent(
                    operationId: command.OperationId,
                    eventTimestamp: _dateService.Now(),
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

                        await UpdateAccount(account, false, r => { }, command.Timestamp);
                    }
                    catch (Exception exception)
                    {
                        await _log.WriteErrorAsync(nameof(DeleteAccountsCommandsHandler), 
                            nameof(MtCoreFinishAccountsDeletionCommand), exception);
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

        private async Task<bool> UpdateAccount(IMarginTradingAccount account, bool toDisablementState,
            Action<string> failHandler, DateTime commandTime)
        {
            try
            {
                await _accountsCacheService.UpdateAccountChanges(account.Id, account.TradingConditionId,
                    account.WithdrawTransferLimit, toDisablementState, 
                    toDisablementState, commandTime, account.AdditionalInfo);
            }
            catch (Exception exception)
            {
                await _log.WriteErrorAsync(nameof(DeleteAccountsCommandsHandler),
                    nameof(DeleteAccountsCommandsHandler), exception.Message, exception);
                failHandler(exception.Message);
                return false;
            }

            return true;
        }
    }
}