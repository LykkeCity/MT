using System;
using System.Collections.Concurrent;
using System.Linq;
using Common;
using Lykke.Common;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services
{
    public class StopOutConsumer : IEventConsumer<StopOutEventArgs>,
        IEventConsumer<LiquidationEndEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IOperationsLogService _operationsLogService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IDateService _dateService;
        private readonly MarginTradingSettings _settings;
        
        private readonly ConcurrentDictionary<string, DateTime> _lastNotifications = 
            new ConcurrentDictionary<string, DateTime>();

        public StopOutConsumer(IThreadSwitcher threadSwitcher,
            IOperationsLogService operationsLogService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IDateService dateService,
            MarginTradingSettings settings)
        {
            _threadSwitcher = threadSwitcher;
            _operationsLogService = operationsLogService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _dateService = dateService;

            _settings = settings;
        }

        int IEventConsumer.ConsumerRank => 100;

        void IEventConsumer<StopOutEventArgs>.ConsumeEvent(object sender, StopOutEventArgs ea)
        {
            var account = ea.Account;
            var eventTime = _dateService.Now();
            var accountMarginEventMessage =
                AccountMarginEventMessageConverter.Create(account, MarginEventTypeContract.Stopout, eventTime);

            _threadSwitcher.SwitchThread(async () =>
            {
                if (_lastNotifications.TryGetValue(account.Id, out var lastNotification)
                    && lastNotification.AddMinutes(_settings.Throttling.StopOutThrottlingPeriodMin) > eventTime)
                {
                    return;
                }
                
                _operationsLogService.AddLog("stopout", account.Id, "", ea.ToJson());

                await _rabbitMqNotifyService.AccountMarginEvent(accountMarginEventMessage);
                
                _lastNotifications.AddOrUpdate(account.Id, eventTime, (s, times) => eventTime);
            });
        }

        public void ConsumeEvent(object sender, LiquidationEndEventArgs ea)
        {
            if (ea.LiquidatedPositionIds.Any())
            {
                _lastNotifications.TryRemove(ea.AccountId, out _);
            }
        }
    }
}