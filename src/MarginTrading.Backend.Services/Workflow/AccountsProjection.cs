using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Core;
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
        private readonly AccountsCacheService _accountsCacheService;
        private readonly IClientNotifyService _clientNotifyService;
        private readonly IEventChannel<AccountBalanceChangedEventArgs> _accountBalanceChangedEventChannel;
        private readonly IConvertService _convertService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly OrdersCache _ordersCache;
        private readonly ILog _log;

        public AccountsProjection(
            AccountsCacheService accountsCacheService, 
            IClientNotifyService clientNotifyService,
            IEventChannel<AccountBalanceChangedEventArgs> accountBalanceChangedEventChannel,
            IConvertService convertService, 
            IAccountUpdateService accountUpdateService,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            OrdersCache ordersCache, 
            ILog log)
        {
            _accountsCacheService = accountsCacheService;
            _clientNotifyService = clientNotifyService;
            _accountBalanceChangedEventChannel = accountBalanceChangedEventChannel;
            _convertService = convertService;
            _accountUpdateService = accountUpdateService;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _ordersCache = ordersCache;
            _log = log;
        }

        /// <summary>
        /// CQRS projection impl
        /// </summary>
        [UsedImplicitly]
        public async Task Handle(AccountChangedEvent e)
        {
            //ensure idempotency
            await _operationExecutionInfoRepository.GetOrAddAsync(e.)
            
            // todo: what happens if events get reordered??
            //TODO make tests on it !!!
            var updatedAccount = Convert(e.Account);

            switch (e.EventType)
            {
                case AccountChangedEventTypeContract.Created:
                    _accountsCacheService.TryAddNew(MarginTradingAccount.Create(updatedAccount));
                    break;
                case AccountChangedEventTypeContract.Updated:
                {
                    var account = _accountsCacheService.TryGet(e.Account.Id);
                    //todo put into account last update time... check it here & in BalanceUpd
                    if (account == null)
                    {
                        _log.WriteWarning(nameof(AccountsProjection), e, $"Account with id {e.Account.Id} was not found");
                        return;
                    }
                    //todo check time in _accountsCacheService (under lock), & upper validation
                    //todo return bool, check here
                    _accountsCacheService.UpdateAccountChanges(updatedAccount.Id, updatedAccount.TradingConditionId,
                        updatedAccount.WithdrawTransferLimit, updatedAccount.IsDisabled);

                    _clientNotifyService.NotifyAccountUpdated(updatedAccount);
                    break;
                }
                case AccountChangedEventTypeContract.BalanceUpdated:
                {
                    if (e.BalanceChange != null)
                    {
                        var account = _accountsCacheService.TryGet(e.Account.Id);
                        //todo put into account last update time... check it here & in BalanceUpd
                        if (account == null)
                        {
                            _log.WriteWarning(nameof(AccountsProjection), e, $"Account with id {e.Account.Id} was not found");
                            return;
                        }
                        
                        _accountsCacheService.UpdateAccountBalance(updatedAccount.Id, updatedAccount.Balance);
                        
                        switch (e.BalanceChange.ReasonType)
                        {
                            case AccountBalanceChangeReasonTypeContract.Withdraw:
                                await _accountUpdateService.UnfreezeWithdrawalMargin(updatedAccount.Id, e.BalanceChange.Id);
                                break;
                            case AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL:
                                if (_ordersCache.Positions.TryGetPositionById(e.BalanceChange.EventSourceId, out var position))
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
                    }
                    else
                    {
                        _log.WriteWarning("AccountChangedEvent Handler", e.ToJson(),
                            $"BalanceChange info is empty");
                    }

                    _accountBalanceChangedEventChannel.SendEvent(this, new AccountBalanceChangedEventArgs(updatedAccount));
                    break;
                }
            }
        }

        private MarginTradingAccount Convert(AccountContract accountContract)
        {
            return _convertService.Convert<AccountContract, MarginTradingAccount>(accountContract,
                o => o.ConfigureMap(MemberList.Source).ForSourceMember(x => x.ModificationTimestamp, c => c.Ignore()));
        }
    }
}