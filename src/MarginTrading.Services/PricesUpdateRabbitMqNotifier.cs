using MarginTrading.Backend.Core;
using MarginTrading.Services.Events;

namespace MarginTrading.Services
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

        int IEventConsumer.ConsumerRank => 100;
        void IEventConsumer<BestPriceChangeEventArgs>.ConsumeEvent(object sender, BestPriceChangeEventArgs ea)
        {
            _rabbitMqNotifyService.OrderBookPrice(ea.BidAskPair);
        }
    }
}