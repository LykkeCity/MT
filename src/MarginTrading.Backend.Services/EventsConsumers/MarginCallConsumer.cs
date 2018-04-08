using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Common;
using Lykke.Common;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services;
using MarginTrading.Common.Services.Client;
using MarginTrading.Common.Settings;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    // TODO: Rename by role
    public class MarginCallConsumer : NotificationSenderBase,
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
        private readonly IDateService _dateService;

        public MarginCallConsumer(IThreadSwitcher threadSwitcher,
            IAppNotifications appNotifications,
            IEmailService emailService,
            IClientAccountService clientAccountService,
            IMarginTradingOperationsLogService operationsLogService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IDateService dateService,
            IAssetsCache assetsCache,
            IAssetPairsCache assetPairsCache)
            : base(appNotifications, clientAccountService, assetsCache, assetPairsCache)
        {
            _threadSwitcher = threadSwitcher;
            _emailService = emailService;
            _clientAccountService = clientAccountService;
            _operationsLogService = operationsLogService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _dateService = dateService;
        }

        int IEventConsumer.ConsumerRank => 103;

        void IEventConsumer<MarginCallEventArgs>.ConsumeEvent(object sender, MarginCallEventArgs ea)
        {
            var account = ea.Account;
            var eventTime = _dateService.Now();
            var accountMarginEventMessage = AccountMarginEventMessageConverter.Create(account, false, eventTime);
            _threadSwitcher.SwitchThread(async () =>
            {
                if (LastNotifications.TryGetValue(account.Id, out var lastNotification)
                    && lastNotification.AddMinutes(NotificationsTimeout) > eventTime)
                {
                    return;
                }

                var marginEventTask = _rabbitMqNotifyService.AccountMarginEvent(accountMarginEventMessage);

                _operationsLogService.AddLog("margin call", account.ClientId, account.Id, "", ea.ToJson());

                var marginUsageLevel = account.GetMarginUsageLevel();
                var marginUsedPerc = marginUsageLevel == 0 ? 0 : 1 / marginUsageLevel;

                var notificationTask = SendMarginEventNotification(account.ClientId, string.Format(
                    MtMessages.Notifications_MarginCall, marginUsedPerc,
                    account.BaseAssetId));

                var clientEmail = await _clientAccountService.GetEmail(account.ClientId);

                var emailTask = !string.IsNullOrEmpty(clientEmail)
                    ? _emailService.SendMarginCallEmailAsync(clientEmail, account.BaseAssetId, account.Id)
                    : Task.CompletedTask;

                await Task.WhenAll(marginEventTask, notificationTask, emailTask);

                LastNotifications.AddOrUpdate(account.Id, eventTime, (s, time) => eventTime);
            });
        }

        public void ConsumeEvent(object sender, OrderPlacedEventArgs ea)
        {
            LastNotifications.TryRemove(ea.Order.AccountId, out var tmp);
        }

        public void ConsumeEvent(object sender, OrderClosedEventArgs ea)
        {
            LastNotifications.TryRemove(ea.Order.AccountId, out var tmp);
        }

        public void ConsumeEvent(object sender, OrderCancelledEventArgs ea)
        {
            LastNotifications.TryRemove(ea.Order.AccountId, out var tmp);
        }
    }
}