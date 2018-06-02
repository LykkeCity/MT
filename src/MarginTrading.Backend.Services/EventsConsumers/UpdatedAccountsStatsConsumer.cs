using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.Backend.Services.EventsConsumers
{
    public class UpdatedAccountsStatsConsumer :
        IEventConsumer<AccountBalanceChangedEventArgs>,
        IEventConsumer<OrderPlacedEventArgs>,
        IEventConsumer<OrderClosedEventArgs>,
        IEventConsumer<OrderCancelledEventArgs>
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly MarginTradingSettings _marginSettings;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;

        public UpdatedAccountsStatsConsumer(IAccountsCacheService accountsCacheService,
            MarginTradingSettings marginSettings,
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
            NotifyAccountStatsChanged(ea.Order.AccountId);
        }

        public void ConsumeEvent(object sender, OrderClosedEventArgs ea)
        {
            NotifyAccountStatsChanged(ea.Order.AccountId);
        }

        public void ConsumeEvent(object sender, OrderCancelledEventArgs ea)
        {
            NotifyAccountStatsChanged(ea.Order.AccountId);
        }

        public int ConsumerRank => 102;

        private void NotifyAccountStatsChanged(IMarginTradingAccount account)
        {
            account.CacheNeedsToBeUpdated();
            
            var stats = account.ToRabbitMqContract(_marginSettings.IsLive);

            _rabbitMqNotifyService.UpdateAccountStats(new AccountStatsUpdateMessage {Accounts = new[] {stats}});
        }
        
        private void NotifyAccountStatsChanged(string accountId)
        {
            var account = _accountsCacheService.Get(accountId);

            NotifyAccountStatsChanged(account);
        }
    }
}
