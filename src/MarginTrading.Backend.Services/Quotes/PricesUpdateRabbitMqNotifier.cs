// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Common.Log;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;

namespace MarginTrading.Backend.Services.Quotes
{
    public class PricesUpdateRabbitMqNotifier : IEventConsumer<BestPriceChangeEventArgs>
    {
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly ILog _log;

        public PricesUpdateRabbitMqNotifier(
            IRabbitMqNotifyService rabbitMqNotifyService,
            ILog log)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _log = log;
        }

        int IEventConsumer.ConsumerRank => 110;
        void IEventConsumer<BestPriceChangeEventArgs>.ConsumeEvent(object sender, BestPriceChangeEventArgs ea)
        {
            PerformanceTracker.TrackAsync("Publish orderbook",
                async () => await _rabbitMqNotifyService.OrderBookPrice(ea.BidAskPair, ea.IsEod), 
                _log).GetAwaiter().GetResult();
        }
    }
}