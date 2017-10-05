using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
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
        private readonly IDateService _dateService;

        public MarginCallConsumer(IThreadSwitcher threadSwitcher,
            IClientSettingsRepository clientSettingsRepository,
            IAppNotifications appNotifications,
            IEmailService emailService,
            IClientAccountService clientAccountService,
            IMarginTradingOperationsLogService operationsLogService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IDateService dateService)
            : base(clientSettingsRepository, appNotifications, clientAccountService)
        {
            _threadSwitcher = threadSwitcher;
            _emailService = emailService;
            _clientAccountService = clientAccountService;
            _operationsLogService = operationsLogService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _dateService = dateService;
        }

        int IEventConsumer.ConsumerRank => 100;

        void IEventConsumer<MarginCallEventArgs>.ConsumeEvent(object sender, MarginCallEventArgs ea)
        {
            var account = ea.Account;
            var eventTime = _dateService.Now();
            var accountMarginEventMessage = AccountMarginEventMessageConverter.Create(account, true, eventTime);
            _threadSwitcher.SwitchThread(async () =>
            {
                if (LastNotifications.TryGetValue(account.Id, out var lastNotification)
                    && lastNotification.AddMinutes(NotificationsTimeout) > eventTime)
                {
                    return;
                }

                var marginEventTask = _rabbitMqNotifyService.AccountMarginEvent(accountMarginEventMessage);

                _operationsLogService.AddLog("margin call", account.ClientId, account.Id, "", ea.ToJson());

                var notificationTask = SendNotification(account.ClientId, string.Format(MtMessages.Notifications_MarginCall, account.GetMarginUsageLevel(),
                        account.BaseAssetId), null);

                var clientAcc = await _clientAccountService.GetAsync(account.ClientId);

                var emailTask = clientAcc != null
                    ? _emailService.SendMarginCallEmailAsync(clientAcc.Email, account.BaseAssetId, account.Id)
                    : Task.CompletedTask;

                await Task.WhenAll(marginEventTask, notificationTask, emailTask);

                LastNotifications.AddOrUpdate(account.Id, eventTime, (s, time) => eventTime);
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