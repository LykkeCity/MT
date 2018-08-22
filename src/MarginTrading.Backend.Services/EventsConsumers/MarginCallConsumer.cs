using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Common;
using Lykke.Common;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services;
using MarginTrading.Common.Services.Client;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    // TODO: Rename by role
    public class MarginCallConsumer : IEventConsumer<MarginCallEventArgs>,
        IEventConsumer<OrderPlacedEventArgs>,
        IEventConsumer<OrderExecutedEventArgs>,
        IEventConsumer<OrderCancelledEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IEmailService _emailService;
        private readonly IClientAccountService _clientAccountService;
        private readonly IOperationsLogService _operationsLogService;
        private static readonly ConcurrentDictionary<string, DateTime> LastNotifications = new ConcurrentDictionary<string, DateTime>();
        private const int NotificationsTimeout = 30;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IDateService _dateService;

        public MarginCallConsumer(IThreadSwitcher threadSwitcher,
            IEmailService emailService,
            IClientAccountService clientAccountService,
            IOperationsLogService operationsLogService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IDateService dateService)
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
            var level = ea.MarginCallLevel == AccountLevel.MarginCall2
                ? MarginEventTypeContract.MarginCall2
                : MarginEventTypeContract.MarginCall1;
            var accountMarginEventMessage = AccountMarginEventMessageConverter.Create(account, level, eventTime);
            _threadSwitcher.SwitchThread(async () =>
            {
                if (LastNotifications.TryGetValue(account.Id, out var lastNotification)
                    && lastNotification.AddMinutes(NotificationsTimeout) > eventTime)
                {
                    return;
                }

                var marginEventTask = _rabbitMqNotifyService.AccountMarginEvent(accountMarginEventMessage);

                _operationsLogService.AddLog($"margin call: {level.ToString()}", account.Id, "", ea.ToJson());

                var clientEmail = await _clientAccountService.GetEmail(account.ClientId);

                var emailTask = !string.IsNullOrEmpty(clientEmail)
                    ? _emailService.SendMarginCallEmailAsync(clientEmail, account.BaseAssetId, account.Id)
                    : Task.CompletedTask;

                await Task.WhenAll(marginEventTask, emailTask);

                LastNotifications.AddOrUpdate(account.Id, eventTime, (s, time) => eventTime);
            });
        }

        public void ConsumeEvent(object sender, OrderPlacedEventArgs ea)
        {
            LastNotifications.TryRemove(ea.Order.AccountId, out var tmp);
        }

        public void ConsumeEvent(object sender, OrderExecutedEventArgs ea)
        {
            LastNotifications.TryRemove(ea.Order.AccountId, out var tmp);
        }

        public void ConsumeEvent(object sender, OrderCancelledEventArgs ea)
        {
            LastNotifications.TryRemove(ea.Order.AccountId, out var tmp);
        }
    }
}