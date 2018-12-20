using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Common;
using Lykke.Common;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services;
using MarginTrading.Common.Services.Client;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    public class MarginCallConsumer : IEventConsumer<MarginCallEventArgs>,
        //IEventConsumer<OrderPlacedEventArgs>,
        IEventConsumer<OrderExecutedEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IEmailService _emailService;
        private readonly IClientAccountService _clientAccountService;
        private readonly IOperationsLogService _operationsLogService;
        private static readonly ConcurrentDictionary<string, (DateTime, DateTime)> LastNotifications = 
            new ConcurrentDictionary<string, (DateTime, DateTime)>();
        private readonly MarginTradingSettings _settings;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IDateService _dateService;

        public MarginCallConsumer(IThreadSwitcher threadSwitcher,
            IEmailService emailService,
            IClientAccountService clientAccountService,
            IOperationsLogService operationsLogService,
            MarginTradingSettings settings,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IDateService dateService)
        {
            _threadSwitcher = threadSwitcher;
            _emailService = emailService;
            _clientAccountService = clientAccountService;
            _operationsLogService = operationsLogService;
            _settings = settings;
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
                    && (level == MarginEventTypeContract.MarginCall1 ? lastNotification.Item1 : lastNotification.Item2)
                        .AddMinutes(_settings.Throttling.MarginCallThrottlingPeriodMin) > eventTime)
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

                var newTimes = level == MarginEventTypeContract.MarginCall1 
                    ? (eventTime, lastNotification.Item2) : (lastNotification.Item1, eventTime);
                LastNotifications.AddOrUpdate(account.Id, newTimes, (s, times) => newTimes);
            });
        }
        
//todo uncomment here, at class registration and in module when MTC-155 task is done 
        /// <summary>
        /// That's for limit orders margin
        /// </summary>
//        public void ConsumeEvent(object sender, OrderPlacedEventArgs ea)
//        {
//            LastNotifications.TryRemove(ea.Order.AccountId, out var tmp);
//        }

        public void ConsumeEvent(object sender, OrderExecutedEventArgs ea)
        {
            LastNotifications.TryRemove(ea.Order.AccountId, out _);
        }
    }
}