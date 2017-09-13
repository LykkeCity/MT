using System;
using System.Collections.Concurrent;
using Common;
using Lykke.Common;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Messages;
using MarginTrading.Services.Events;
using MarginTrading.Services.Notifications;

namespace MarginTrading.Services
{
    // TODO: Rename by role
    public class MarginCallConsumer : SendNotificationBase,
        IEventConsumer<MarginCallEventArgs>,
        IEventConsumer<OrderPlacedEventArgs>,
        IEventConsumer<OrderClosedEventArgs>,
        IEventConsumer<OrderCancelledEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IEmailService _emailService;
        private readonly IClientAccountService _clientAccountService;
        private readonly IMarginTradingOperationsLogService _operationsLogService;
        private static readonly ConcurrentDictionary<string, DateTime> LastNotifications = new ConcurrentDictionary<string, DateTime>();
        private const int NotificationsTimeout = 30;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;

        public MarginCallConsumer(IThreadSwitcher threadSwitcher,
            IClientSettingsRepository clientSettingsRepository,
            IAppNotifications appNotifications,
            IEmailService emailService,
            IClientAccountService clientAccountService,
            IMarginTradingOperationsLogService operationsLogService,
            IRabbitMqNotifyService rabbitMqNotifyService)
            : base(clientSettingsRepository, appNotifications, clientAccountService)
        {
            _threadSwitcher = threadSwitcher;
            _emailService = emailService;
            _clientAccountService = clientAccountService;
            _operationsLogService = operationsLogService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
        }

        int IEventConsumer.ConsumerRank => 100;

        void IEventConsumer<MarginCallEventArgs>.ConsumeEvent(object sender, MarginCallEventArgs ea)
        {
            var account = ea.Account;
            _threadSwitcher.SwitchThread(async () =>
            {
                var now = DateTime.UtcNow;
                DateTime lastNotification;
                if (LastNotifications.TryGetValue(account.Id, out lastNotification))
                {
                    if (lastNotification.AddMinutes(NotificationsTimeout) > now)
                        return;
                }

                _operationsLogService.AddLog("margin call", account.ClientId, account.Id, "", ea.ToJson());

                await SendNotification(account.ClientId, string.Format(MtMessages.Notifications_MarginCall, account.GetMarginUsageLevel(),
                        account.BaseAssetId), null);

                var clientAcc = await _clientAccountService.GetAsync(account.ClientId);

                if (clientAcc != null)
                    await _emailService.SendMarginCallEmailAsync(clientAcc.Email, account.BaseAssetId, account.Id);

                LastNotifications.AddOrUpdate(account.Id, now, (s, time) => now);

                await _rabbitMqNotifyService.AccountMarginEvent(account, true, now);
            });
        }

        public void ConsumeEvent(object sender, OrderPlacedEventArgs ea)
        {
            LastNotifications.TryRemove(ea.Order.AccountId, out DateTime tmp);
        }

        public void ConsumeEvent(object sender, OrderClosedEventArgs ea)
        {
            LastNotifications.TryRemove(ea.Order.AccountId, out DateTime tmp);
        }

        public void ConsumeEvent(object sender, OrderCancelledEventArgs ea)
        {
            LastNotifications.TryRemove(ea.Order.AccountId, out DateTime tmp);
        }
    }
}