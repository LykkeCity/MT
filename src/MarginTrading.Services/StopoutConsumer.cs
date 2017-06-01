using System.Linq;
using Lykke.Common;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Messages;
using MarginTrading.Services.Events;
using MarginTrading.Services.Notifications;

namespace MarginTrading.Services
{
    // TODO: Rename by role
    public class StopOutConsumer : SendNotificationBase,
        IEventConsumer<StopOutEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IClientNotifyService _notifyService;

        public StopOutConsumer(IThreadSwitcher threadSwitcher,
            IClientSettingsRepository clientSettingsRepository,
            IAppNotifications appNotifications,
            IClientAccountService clientAccountService,
            IClientNotifyService notifyService) : base(clientSettingsRepository,
            appNotifications,
            clientAccountService)
        {
            _threadSwitcher = threadSwitcher;
            _notifyService = notifyService;
        }

        int IEventConsumer.ConsumerRank => 100;
        void IEventConsumer<StopOutEventArgs>.ConsumeEvent(object sender, StopOutEventArgs ea)
        {
            var account = ea.Account;
            var orders = ea.Orders;
            _threadSwitcher.SwitchThread(async () =>
            {
                var totalPnl = orders.Sum(x => x.GetFpl());

                _notifyService.NotifyAccountStopout(account.ClientId, account.Id, orders.Length, totalPnl);
                _notifyService.NotifyAccountChanged(account);

                await SendNotification(account.ClientId, string.Format(MtMessages.Notifications_StopOutNotification, orders.Length, totalPnl, account.BaseAssetId), null);
            });
        }
    }
}