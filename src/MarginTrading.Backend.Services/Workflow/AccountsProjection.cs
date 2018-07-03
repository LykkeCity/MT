using AutoMapper;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Core;
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
        private readonly IEventChannel<AccountBalanceChangedEventArgs> _acountBalanceChangedEventChannel;
        private readonly IConvertService _convertService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly OrdersCache _ordersCache;
        private readonly ILog _log;

        public AccountsProjection(AccountsCacheService accountsCacheService, IClientNotifyService clientNotifyService,
            IEventChannel<AccountBalanceChangedEventArgs> acountBalanceChangedEventChannel,
            IConvertService convertService, IAccountUpdateService accountUpdateService,
            OrdersCache ordersCache, ILog log)
        {
            _accountsCacheService = accountsCacheService;
            _clientNotifyService = clientNotifyService;
            _acountBalanceChangedEventChannel = acountBalanceChangedEventChannel;
            _convertService = convertService;
            _accountUpdateService = accountUpdateService;
            _ordersCache = ordersCache;
            _log = log;
        }

        /// <summary>
        /// CQRS projection impl
        /// </summary>
        [UsedImplicitly]
        public void Handle(AccountChangedEvent e)
        {
            // todo: what happens if events get reordered??
            var updatedAccount = Convert(e.Account);

            if (e.EventType == AccountChangedEventTypeContract.Created)
            {
                _accountsCacheService.TryAddNew(MarginTradingAccount.Create(updatedAccount));
            }
            else
            {
                _accountsCacheService.UpdateAccountChanges(updatedAccount.Id, updatedAccount.TradingConditionId,
                    updatedAccount.Balance, updatedAccount.WithdrawTransferLimit);

                _clientNotifyService.NotifyAccountUpdated(updatedAccount);
            }

            if (e.EventType == AccountChangedEventTypeContract.BalanceUpdated)
            {
                if (e.BalanceChange != null)
                {
                    switch (e.BalanceChange.ReasonType)
                    {
                        case AccountBalanceChangeReasonTypeContract.Withdraw:
                            _accountUpdateService.UnfreezeWithdrawalMargin(updatedAccount.Id, e.BalanceChange.Id);
                            
                            break;

                        case AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL:
                            
                            if (_ordersCache.Positions.TryGetOrderById(e.BalanceChange.EventSourceId, out var position))
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

                _acountBalanceChangedEventChannel.SendEvent(this, new AccountBalanceChangedEventArgs(updatedAccount));
            }
        }

        private MarginTradingAccount Convert(AccountContract accountContract)
        {
            return _convertService.Convert<AccountContract, MarginTradingAccount>(accountContract,
                o => o.ConfigureMap(MemberList.Source).ForSourceMember(x => x.ModificationTimestamp, c => c.Ignore()));
        }
    }
}