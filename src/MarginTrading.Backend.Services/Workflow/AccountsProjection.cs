using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    /// <summary>
    /// Listens to <see cref="AccountChangedEvent"/>s and builds a projection inside of the
    /// <see cref="IAccountsCacheService"/>
    /// </summary>
    public class AccountsProjection
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IClientNotifyService _clientNotifyService;
        private readonly IEventChannel<AccountBalanceChangedEventArgs> _accountBalanceChangedEventChannel;
        private readonly IConvertService _convertService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly IDateService _dateService;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly IChaosKitty _chaosKitty;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly OrdersCache _ordersCache;
        private readonly ILog _log;

        private const string OperationName = "AccountsProjection";
        
        public AccountsProjection(
            IAccountsCacheService accountsCacheService, 
            IClientNotifyService clientNotifyService,
            IEventChannel<AccountBalanceChangedEventArgs> accountBalanceChangedEventChannel,
            IConvertService convertService, 
            IAccountUpdateService accountUpdateService,
            IDateService dateService,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            IChaosKitty chaosKitty,
            IIdentityGenerator identityGenerator,
            OrdersCache ordersCache, 
            ILog log)
        {
            _accountsCacheService = accountsCacheService;
            _clientNotifyService = clientNotifyService;
            _accountBalanceChangedEventChannel = accountBalanceChangedEventChannel;
            _convertService = convertService;
            _accountUpdateService = accountUpdateService;
            _dateService = dateService;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _identityGenerator = identityGenerator;
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
            //todo introduce operationId in AccountChangeEvent instead of that
            var operationId = e.BalanceChange?.Id ?? (e.EventType == AccountChangedEventTypeContract.Created
                                  ? e.Account.Id
                                  : $"{e.Account.Id}-update-{_identityGenerator.GenerateGuid()}");
            
            //ensure idempotency
            var executionInfo = await _operationExecutionInfoRepository.GetOrAddAsync(
                operationName: OperationName,
                operationId: operationId,
                factory: () => new OperationExecutionInfo<OperationData>(
                    operationName: OperationName,
                    id: operationId,
                    lastModified: _dateService.Now(),
                    data: new OperationData { State = OperationState.Initiated }
                ));
            
            // todo: what happens if events get reordered??

            if (executionInfo.Data.SwitchState(OperationState.Initiated, OperationState.Finished))
            {
                var updatedAccount = Convert(e.Account);

                switch (e.EventType)
                {
                    case AccountChangedEventTypeContract.Created:
                        _accountsCacheService.TryAddNew(MarginTradingAccount.Create(updatedAccount));
                        break;
                    case AccountChangedEventTypeContract.Updated:
                    {
                        var account = _accountsCacheService.TryGet(e.Account.Id);
                        if (ValidateAccount(account, e))
                        {
                            _accountsCacheService.UpdateAccountChanges(updatedAccount.Id,
                                updatedAccount.TradingConditionId,
                                updatedAccount.WithdrawTransferLimit, updatedAccount.IsDisabled);

                            _clientNotifyService.NotifyAccountUpdated(updatedAccount);
                        }

                        break;
                    }
                    case AccountChangedEventTypeContract.BalanceUpdated:
                    {
                        if (e.BalanceChange != null)
                        {
                            var account = _accountsCacheService.TryGet(e.Account.Id);
                            if (ValidateAccount(account, e))
                            {
                                _accountsCacheService.UpdateAccountBalance(updatedAccount.Id, updatedAccount.Balance);

                                switch (e.BalanceChange.ReasonType)
                                {
                                    case AccountBalanceChangeReasonTypeContract.Withdraw:
                                        await _accountUpdateService.UnfreezeWithdrawalMargin(updatedAccount.Id,
                                            e.BalanceChange.Id);
                                        break;
                                    case AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL:
                                        if (_ordersCache.Positions.TryGetPositionById(e.BalanceChange.EventSourceId,
                                            out var position))
                                        {
                                            position.ChargePnL(e.BalanceChange.Id, e.BalanceChange.ChangeAmount);
                                        }
                                        else
                                        {
                                            _log.WriteWarning("AccountChangedEvent Handler", e.ToJson(),
                                                $"Position [{e.BalanceChange.EventSourceId} was not found]");
                                        }

                                        break;
                                }

                                _accountBalanceChangedEventChannel.SendEvent(this,
                                    new AccountBalanceChangedEventArgs(updatedAccount));
                            }
                        }
                        else
                        {
                            _log.WriteWarning("AccountChangedEvent Handler", e.ToJson(), "BalanceChange info is empty");
                        }

                        break;
                    }
                }
                
                _chaosKitty.Meow(operationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        private bool ValidateAccount(IMarginTradingAccount account, AccountChangedEvent e)
        {
            if (account == null)
            {
                _log.WriteWarning(nameof(AccountsProjection), e, $"Account with id {e.Account.Id} was not found");
                return false;
            }

            if (!_accountsCacheService.CheckEventTimeNewer(account.Id, e.ChangeTimestamp))
            {
                _log.WriteInfo(nameof(AccountsProjection), e, $"Account with id {e.Account.Id} is in newer state that the event");
                return false;
            }

            return true;
        }

        private MarginTradingAccount Convert(AccountContract accountContract)
        {
            return _convertService.Convert<AccountContract, MarginTradingAccount>(accountContract,
                o => o.ConfigureMap(MemberList.Source).ForSourceMember(x => x.ModificationTimestamp, c => c.Ignore()));
        }
    }
}