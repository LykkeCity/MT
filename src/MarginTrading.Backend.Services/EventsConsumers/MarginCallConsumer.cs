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
    public class MarginCallConsumer : IEventConsumer<MarginCallEventArgs>
        //IEventConsumer<OrderPlacedEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IEmailService _emailService;
        private readonly IClientAccountService _clientAccountService;
        private readonly IOperationsLogService _operationsLogService;
        private readonly MarginTradingSettings _settings;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IDateService _dateService;
        
        private readonly ConcurrentDictionary<string, DateTime> _mc1LastNotifications = 
            new ConcurrentDictionary<string, DateTime>();
        private readonly ConcurrentDictionary<string, DateTime> _mc2LastNotifications = 
            new ConcurrentDictionary<string, DateTime>();
        private readonly ConcurrentDictionary<string, DateTime> _overnightMcLastNotifications = 
            new ConcurrentDictionary<string, DateTime>();

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
            var (level, lastNotifications) = LevelAndNotificationsCache(ea.MarginCallLevel);
            
            if (lastNotifications == null)
            {
                return;
            }
            
            var accountMarginEventMessage = AccountMarginEventMessageConverter.Create(account, level, eventTime);
            
            _threadSwitcher.SwitchThread(async () =>
            {
                if (lastNotifications.TryGetValue(account.Id, out var lastNotification)
                    && lastNotification.AddMinutes(_settings.Throttling.MarginCallThrottlingPeriodMin) > eventTime)
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

                lastNotifications.AddOrUpdate(account.Id, eventTime, (s, times) => eventTime);
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

        private (MarginEventTypeContract, ConcurrentDictionary<string, DateTime>) LevelAndNotificationsCache(AccountLevel level)
        {
            switch (level)
            {
                case AccountLevel.MarginCall1: return (MarginEventTypeContract.MarginCall1, _mc1LastNotifications);
                case AccountLevel.MarginCall2: return (MarginEventTypeContract.MarginCall2, _mc2LastNotifications);
                case AccountLevel.OvernightMarginCall: return (MarginEventTypeContract.OvernightMarginCall, _overnightMcLastNotifications);
                default: return (default, null);
            }
        }
    }
}