﻿using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.Backend.Services
{
    public class UpdatedAccountsStatsConsumer :
        IEventConsumer<AccountBalanceChangedEventArgs>,
        IEventConsumer<OrderPlacedEventArgs>,
        IEventConsumer<OrderClosedEventArgs>,
        IEventConsumer<OrderCancelledEventArgs>
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly MarginSettings _marginSettings;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;

        public UpdatedAccountsStatsConsumer(IAccountsCacheService accountsCacheService,
            MarginSettings marginSettings,
            IRabbitMqNotifyService rabbitMqNotifyService)
        {
            _accountsCacheService = accountsCacheService;
            _marginSettings = marginSettings;
            _rabbitMqNotifyService = rabbitMqNotifyService;
        }
        
        public void ConsumeEvent(object sender, AccountBalanceChangedEventArgs ea)
        {
            NotifyAccountStatsChanged(ea.Account);
        }

        public void ConsumeEvent(object sender, OrderPlacedEventArgs ea)
        {
            NotifyAccountStatsChanged(ea.Order.ClientId, ea.Order.AccountId);
        }

        public void ConsumeEvent(object sender, OrderClosedEventArgs ea)
        {
            NotifyAccountStatsChanged(ea.Order.ClientId, ea.Order.AccountId);
        }

        public void ConsumeEvent(object sender, OrderCancelledEventArgs ea)
        {
            NotifyAccountStatsChanged(ea.Order.ClientId, ea.Order.AccountId);
        }

        public int ConsumerRank => 101;

        private void NotifyAccountStatsChanged(IMarginTradingAccount account)
        {
            var stats = account.ToRabbitMqContract(_marginSettings.IsLive);

            _rabbitMqNotifyService.UpdateAccountStats(new AccountStatsUpdateMessage {Accounts = new[] {stats}});
        }
        
        private void NotifyAccountStatsChanged(string clientId, string accountId)
        {
            var account = _accountsCacheService.Get(clientId, accountId);

            NotifyAccountStatsChanged(account);
        }
    }
}
