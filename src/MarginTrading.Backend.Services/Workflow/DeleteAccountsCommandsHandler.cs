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
        private readonly ICqrsSender _cqrsSender;
        private readonly IChaosKitty _chaosKitty;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly ILog _log;
        
        private const string OperationName = "DeleteAccounts";
        
        public DeleteAccountsCommandsHandler(
            IOrderReader orderReader,
            IDateService dateService,
            IAccountsCacheService accountsCacheService,
            ITradingEngine tradingEngine,
            ICqrsSender cqrsSender,
            IChaosKitty chaosKitty,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            ILog log)
        {
            _orderReader = orderReader;
            _dateService = dateService;
            _accountsCacheService = accountsCacheService;
            _tradingEngine = tradingEngine;
            _cqrsSender = cqrsSender;
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

                    var orders = _orderReader.GetPending().Where(x => x.AccountId == accountId).ToList();
                    if (orders.Any())
                    {
                        var (failedToCloseOrderId, failReason) = ((string)null, (string)null);
                        foreach (var order in orders)
                        {
                            try
                            {
                                _tradingEngine.CancelPendingOrder(order.Id, order.AdditionalInfo,command.OperationId, 
                                $"{nameof(DeleteAccountsCommandsHandler)}: force close all orders."); 
                            }
                            catch (Exception exception)
                            {
                                failedToCloseOrderId = order.Id;
                                failReason = exception.Message;
                            }
                        }

                        if (failedToCloseOrderId != null)
                        {
                            executionInfo.Data.DontUnblockAccounts.Add(accountId);
                            failedAccounts.Add(accountId, $"Account contain some orders which failed to be closed. First one [{failedToCloseOrderId}]: {failReason}. Account is blocked.");
                            await UpdateAccount(account, true, r => { });
                            continue;
                        }
                    }
                    
                    var positionsCount = _orderReader.GetPositions().Count(x => x.AccountId == accountId);                    
                    if (positionsCount != 0)
                    {
                        _cqrsSender.SendCommandToSelf(new StartLiquidationInternalCommand
                        {
                            OperationId = $"{command.OperationId}_{accountId}",
                            AccountId = account.Id,
                            CreationTime = _dateService.Now(),
                            QuoteInfo = null,
                            Direction = null,
                            LiquidationType = LiquidationType.Forced,
                        });
                        executionInfo.Data.DontUnblockAccounts.Add(accountId);
                        failedAccounts.Add(accountId, $"Account contain {positionsCount} open positions which must be closed before account deletion. Liquidation started, account is blocked. Try deleting an account tomorrow.");
                        await UpdateAccount(account, true, r => { });
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

                    if (!await UpdateAccount(account, true, r => failedAccounts.Add(accountId, r)))
                    {
                        continue;
                    }
                }

                publisher.PublishEvent(new AccountsBlockedForDeletionEvent(
                    operationId: command.OperationId,
                    eventTimestamp: _dateService.Now(),
                    blockedAccountIds: command.AccountIds.Except(failedAccounts.Keys).ToList(),
                    failedAccountIds: failedAccounts
                ));
                //todo send dontUnblockAccounts here
                
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
                foreach (var failedAccountId in command.FailedAccountIds.Except(executionInfo.Data.DontUnblockAccounts))
                {
                    var account = _accountsCacheService.Get(failedAccountId);
                    
                    await UpdateAccount(account, false, r => { });
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
            Action<string> failHandler)
        {
            try
            {
                await _accountsCacheService.UpdateAccountChanges(account.Id, account.TradingConditionId,
                    account.WithdrawTransferLimit, toDisablementState, 
                    toDisablementState, _dateService.Now());
            }
            catch (Exception exception)
            {
                _log.Error(nameof(DeleteAccountsCommandsHandler), exception, exception.Message);
                failHandler(exception.Message);
                return false;
            }

            return true;
        }
    }
}