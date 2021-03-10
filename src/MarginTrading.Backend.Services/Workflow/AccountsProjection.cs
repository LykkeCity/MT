// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Common.Log;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.Workflow
{
    /// <summary>
    /// Listens to <see cref="AccountChangedEvent"/>s and builds a projection inside of the
    /// <see cref="IAccountsCacheService"/>
    /// </summary>
    public class AccountsProjection
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IEventChannel<AccountBalanceChangedEventArgs> _accountBalanceChangedEventChannel;
        private readonly IConvertService _convertService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly IDateService _dateService;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly IChaosKitty _chaosKitty;
        private readonly OrdersCache _ordersCache;
        private readonly ILog _log;

        private const string OperationName = "AccountsProjection";

        public AccountsProjection(
            IAccountsCacheService accountsCacheService,
            IEventChannel<AccountBalanceChangedEventArgs> accountBalanceChangedEventChannel,
            IConvertService convertService,
            IAccountUpdateService accountUpdateService,
            IDateService dateService,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            IChaosKitty chaosKitty,
            OrdersCache ordersCache,
            ILog log)
        {
            _accountsCacheService = accountsCacheService;
            _accountBalanceChangedEventChannel = accountBalanceChangedEventChannel;
            _convertService = convertService;
            _accountUpdateService = accountUpdateService;
            _dateService = dateService;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _chaosKitty = chaosKitty;
            _ordersCache = ordersCache;
            _log = log;
        }

        /// <summary>
        /// CQRS projection impl
        /// </summary>
        [UsedImplicitly]
        public async Task Handle(AccountChangedEvent e)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetOrAddAsync(
                operationName: OperationName,
                operationId: e.OperationId,
                factory: () => new OperationExecutionInfo<OperationData>(
                    operationName: OperationName,
                    id: e.OperationId,
                    lastModified: _dateService.Now(),
                    data: new OperationData { State = OperationState.Initiated }
                ));

            if (executionInfo.Data.SwitchState(OperationState.Initiated, OperationState.Finished))
            {
                var updatedAccount = Convert(e.Account, e.ChangeTimestamp);

                switch (e.EventType)
                {
                    case AccountChangedEventTypeContract.Created:
                        _accountsCacheService.TryAddNew(MarginTradingAccount.Create(updatedAccount));
                        break;
                    case AccountChangedEventTypeContract.Updated:
                        {
                            var account = _accountsCacheService.TryGet(e.Account.Id);
                            if (await ValidateAccount(account, e)
                                && await _accountsCacheService.UpdateAccountChanges(updatedAccount.Id,
                                    updatedAccount.TradingConditionId, updatedAccount.WithdrawTransferLimit,
                                    updatedAccount.IsDisabled, updatedAccount.IsWithdrawalDisabled, e.ChangeTimestamp, updatedAccount.AdditionalInfo))
                            {
                                _accountUpdateService.RemoveLiquidationStateIfNeeded(e.Account.Id,
                                    "Trading conditions changed");
                            }
                            break;
                        }
                    case AccountChangedEventTypeContract.BalanceUpdated:
                        {
                            if (e.BalanceChange != null)
                            {
                                var account = _accountsCacheService.TryGet(e.Account.Id);
                                if (await ValidateAccount(account, e))
                                {
                                    switch (e.BalanceChange.ReasonType)
                                    {
                                        case AccountBalanceChangeReasonTypeContract.Withdraw:
                                            await _accountUpdateService.UnfreezeWithdrawalMargin(updatedAccount.Id,
                                                e.BalanceChange.Id);
                                            break;
                                        case AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL:
                                            HandleUnrealizedPnLTransaction(e);
                                            break;
                                        case AccountBalanceChangeReasonTypeContract.RealizedPnL:
                                            await _accountUpdateService.UnfreezeUnconfirmedMargin(e.Account.Id,
                                                e.BalanceChange.EventSourceId);
                                            break;
                                        case AccountBalanceChangeReasonTypeContract.Reset:
                                            await HandleAccountReset(e);
                                            break;
                                    }

                                    await _accountsCacheService.UpdateAccountBalance(updatedAccount.Id,
                                        e.BalanceChange.Balance, e.ChangeTimestamp);

                                    _accountUpdateService.RemoveLiquidationStateIfNeeded(e.Account.Id,
                                        "Balance updated");

                                    _accountBalanceChangedEventChannel.SendEvent(this,
                                        new AccountBalanceChangedEventArgs(updatedAccount.Id));
                                }
                            }
                            else
                            {
                                _log.WriteWarning("AccountChangedEvent Handler", e.ToJson(), "BalanceChange info is empty");
                            }

                            break;
                        }
                    case AccountChangedEventTypeContract.Deleted:
                        //account deletion from cache is double-handled by CQRS flow
                        _accountsCacheService.Remove(e.Account.Id);
                        break;

                    default:
                        await _log.WriteErrorAsync(nameof(AccountsProjection), nameof(AccountChangedEvent),
                            e.ToJson(), new Exception("AccountChangedEventTypeContract was in incorrect state"));
                        break;
                }

                _chaosKitty.Meow(e.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        private async Task HandleAccountReset(AccountChangedEvent e)
        {
            foreach (var pos in _ordersCache.Positions.GetPositionsByAccountIds(
                e.Account.Id))
            {
                _ordersCache.Positions.Remove(pos);
            }

            foreach (var order in _ordersCache.Active.GetOrdersByAccountIds(e.Account.Id))
            {
                _ordersCache.Active.Remove(order);
            }

            foreach (var order in _ordersCache.Inactive.GetOrdersByAccountIds(e.Account.Id))
            {
                _ordersCache.Inactive.Remove(order);
            }

            foreach (var order in _ordersCache.InProgress.GetOrdersByAccountIds(
                e.Account.Id))
            {
                _ordersCache.InProgress.Remove(order);
            }

            var warnings = _accountsCacheService.Reset(e.Account.Id, e.ChangeTimestamp);
            if (!string.IsNullOrEmpty(warnings))
            {
                await _log.WriteWarningAsync(nameof(AccountChangedEvent),
                    nameof(AccountBalanceChangeReasonTypeContract.Reset),
                    warnings);
            }

            await _log.WriteInfoAsync(nameof(AccountChangedEvent),
                nameof(AccountBalanceChangeReasonTypeContract.Reset),
                $"Account {e.Account.Id} was reset.");
        }

        private void HandleUnrealizedPnLTransaction(AccountChangedEvent e)
        {
            if (_ordersCache.Positions.TryGetPositionById(e.BalanceChange.EventSourceId,
                out var position))
            {
                UnrealizedPnlMetadataContract metadata = null;

                if (!string.IsNullOrWhiteSpace(e.BalanceChange.AuditLog))
                {
                    try
                    {
                        metadata = e.BalanceChange.AuditLog
                            .DeserializeJson<UnrealizedPnlMetadataContract>();
                    }
                    catch
                    {
                        _log.WriteWarning("AccountChangedEvent Handler", e.ToJson(),
                            $"Metadata for {AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL} not found");
                    }
                }

                //let's keep it for backward compatibility and for unexpected errors
                if (metadata == null || metadata.RawTotalPnl == default)
                {
                    position.ChargePnL(e.BalanceChange.Id, e.BalanceChange.ChangeAmount);
                }
                else
                {
                    position.SetChargedPnL(e.BalanceChange.Id, metadata.RawTotalPnl);
                }
            }
            else
            {
                _log.WriteWarning("AccountChangedEvent Handler", e.ToJson(),
                    $"Position [{e.BalanceChange.EventSourceId} was not found]");
            }
        }

        private async Task<bool> ValidateAccount(IMarginTradingAccount account, AccountChangedEvent e)
        {
            if (account == null)
            {
                await _log.WriteWarningAsync(nameof(AccountsProjection), e.ToJson(),
                    $"Account with id {e.Account.Id} was not found");
                return false;
            }

            return true;
        }

        private MarginTradingAccount Convert(AccountContract accountContract, DateTime eventTime)
        {
            var retVal = _convertService.Convert<AccountContract, MarginTradingAccount>(accountContract,
                o => o.ConfigureMap(MemberList.Source)
                    .ForSourceMember(x => x.ModificationTimestamp, c => c.Ignore()));
            retVal.LastBalanceChangeTime = eventTime;
            return retVal;
        }
    }
}