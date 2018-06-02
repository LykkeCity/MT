using AutoMapper;
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

        public AccountsProjection(AccountsCacheService accountsCacheService, IClientNotifyService clientNotifyService,
            IEventChannel<AccountBalanceChangedEventArgs> acountBalanceChangedEventChannel,
            IConvertService convertService)
        {
            _accountsCacheService = accountsCacheService;
            _clientNotifyService = clientNotifyService;
            _acountBalanceChangedEventChannel = acountBalanceChangedEventChannel;
            _convertService = convertService;
        }

        /// <summary>
        /// CQRS projection impl
        /// </summary>
        [UsedImplicitly]
        public void Handle(AccountChangedEvent e)
        {
            // todo: what happens if events get reordered??
            var updatedAccount = Convert(e.Account);
            _accountsCacheService.Update(updatedAccount);
            _clientNotifyService.NotifyAccountUpdated(updatedAccount);
            if (e.EventType == AccountChangedEventTypeContract.BalanceUpdated)
                _acountBalanceChangedEventChannel.SendEvent(this, new AccountBalanceChangedEventArgs(updatedAccount));
        }

        private MarginTradingAccount Convert(AccountContract accountContract)
        {
            return _convertService.Convert<AccountContract, MarginTradingAccount>(accountContract,
                o => o.ConfigureMap(MemberList.Source).ForSourceMember(x => x.ModificationTimestamp, c => c.Ignore()));
        }
    }
}