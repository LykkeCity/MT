using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;

namespace MarginTrading.Backend.Services.Quotes
{
    public class PricesUpdateRabbitMqNotifier : IEventConsumer<BestPriceChangeEventArgs>
    {
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;

        public PricesUpdateRabbitMqNotifier(
            IRabbitMqNotifyService rabbitMqNotifyService
        )
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
        }

        int IEventConsumer.ConsumerRank => 110;
        void IEventConsumer<BestPriceChangeEventArgs>.ConsumeEvent(object sender, BestPriceChangeEventArgs ea)
        {
            _rabbitMqNotifyService.OrderBookPrice(ea.BidAskPair);
        }
    }
}