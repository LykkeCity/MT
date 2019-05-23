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
        IEventConsumer<OrderExecutedEventArgs>,
        IEventConsumer<OrderCancelledEventArgs>
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;

        public UpdatedAccountsStatsConsumer(IAccountsCacheService accountsCacheService,
            IRabbitMqNotifyService rabbitMqNotifyService)
        {
            _accountsCacheService = accountsCacheService;
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

        public void ConsumeEvent(object sender, OrderExecutedEventArgs ea)
        {
            NotifyAccountStatsChanged(ea.Order.AccountId);
        }

        public void ConsumeEvent(object sender, OrderCancelledEventArgs ea)
        {
            NotifyAccountStatsChanged(ea.Order.AccountId);
        }

        public int ConsumerRank => 102;

        private void NotifyAccountStatsChanged(MarginTradingAccount account)
        {
            account.CacheNeedsToBeUpdated();
            
            // not needed right now
            
            //var stats = account.ToRabbitMqContract();

            //_rabbitMqNotifyService.UpdateAccountStats(new AccountStatsUpdateMessage {Accounts = new[] {stats}});
        }
        
        private void NotifyAccountStatsChanged(string accountId)
        {
            var account = _accountsCacheService.Get(accountId);

            NotifyAccountStatsChanged(account);
        }
    }
}
