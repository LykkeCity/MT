using MarginTrading.Core;
using MarginTrading.Services.Events;
using WampSharp.V2.Realm;

namespace MarginTrading.Services
{
    public class OrderBookChangedConsumer : IEventConsumer<OrderBookChangeEventArgs>
    {
        private readonly IWampHostedRealm _realm;

        public OrderBookChangedConsumer(IWampHostedRealm realm)
        {
            _realm = realm;
        }

        public void ConsumeEvent(object sender, OrderBookChangeEventArgs ea)
        {
            foreach (var level in ea.Buy)
            {
                var subject = _realm.Services.GetSubject<OrderBookLevel>($"orderbook.update.{level.Key}");
                foreach (var orderBookLevel in level.Value)
                    subject.OnNext(orderBookLevel.Value);
            }

            foreach (var level in ea.Sell)
            {
                var subject = _realm.Services.GetSubject<OrderBookLevel>($"orderbook.update.{level.Key}");
                foreach (var orderBookLevel in level.Value)
                    subject.OnNext(orderBookLevel.Value);
            }
        }

        public int ConsumerRank => 100;
    }
}
