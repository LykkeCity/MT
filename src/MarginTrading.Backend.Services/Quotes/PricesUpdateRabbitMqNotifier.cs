// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;

namespace MarginTrading.Backend.Services.Quotes
{
    public class PricesUpdateRabbitMqNotifier : IEventConsumer<BestPriceChangeEventArgs>
    {
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;

        public PricesUpdateRabbitMqNotifier(IRabbitMqNotifyService rabbitMqNotifyService)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
        }

        int IEventConsumer.ConsumerRank => 110;
        void IEventConsumer<BestPriceChangeEventArgs>.ConsumeEvent(object sender, BestPriceChangeEventArgs ea)
        {
            PerformanceTracker.TrackAsync("PublishBestPrice",
                async () => await _rabbitMqNotifyService.OrderBookPrice(ea.BidAskPair, ea.IsEod),
                ea.BidAskPair.Instrument).GetAwaiter().GetResult();
        }
    }
}