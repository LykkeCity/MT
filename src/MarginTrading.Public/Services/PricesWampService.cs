using System;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Common.RabbitMq;
using System.Reactive.Subjects;
using MarginTrading.Core;
using MarginTrading.Public.Settings;
using WampSharp.V2.Realm;

namespace MarginTrading.Public.Services
{
    public class PricesWampService : IStartable, IDisposable
    {
        private readonly MtPublicBaseSettings _settings;
        private readonly ILog _log;
        private RabbitMqSubscriber<InstrumentBidAskPair> _subscriber;
        private readonly ISubject<InstrumentBidAskPair> _subject;

        public PricesWampService(
            MtPublicBaseSettings settings,
            IWampHostedRealm realm,
            ILog log)
        {
            _settings = settings;
            _subject = realm.Services.GetSubject<InstrumentBidAskPair>(_settings.WampPricesTopicName);
            _log = log;
        }

        public void Start()
        {
            _subscriber = new RabbitMqSubscriber<InstrumentBidAskPair>(new RabbitMqSubscriberSettings
            {
                ConnectionString = _settings.MtRabbitMqConnString,
                ExchangeName = _settings.RabbitMqQueues.OrderbookPrices.ExchangeName,
                QueueName = _settings.RabbitMqQueues.OrderbookPrices.QueueName + $".public.{nameof(PricesWampService).ToLower()}",
                IsDurable = false
            })
                .SetMessageDeserializer(new FrontEndDeserializer<InstrumentBidAskPair>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetLogger(_log)
                .Subscribe(ProcessPrice)
                .Start();
        }

        public void Dispose()
        {
            _subscriber.Stop();
        }

        private Task ProcessPrice(InstrumentBidAskPair instrumentBidAskPair)
        {
            _subject.OnNext(instrumentBidAskPair);
            return Task.FromResult(0);
        }
    }
}
