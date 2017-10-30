using System;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Common.RabbitMq;
using System.Collections.Generic;
using System.Linq;
using Lykke.RabbitMqBroker;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.Public.Settings;

namespace MarginTrading.Public.Services
{
    public interface IPricesCacheService
    {
        InstrumentBidAskPairContract[] GetPrices();
    }

    public class PricesCacheService : IPricesCacheService, IStartable, IDisposable
    {
        private readonly MtPublicBaseSettings _settings;
        private readonly ILog _log;
        private RabbitMqSubscriber<InstrumentBidAskPairContract> _subscriber;
        private readonly Dictionary<string, InstrumentBidAskPairContract> _lastPrices;

        public PricesCacheService(MtPublicBaseSettings settings,
            ILog log)
        {
            _settings = settings;
            _log = log;
            _lastPrices = new Dictionary<string, InstrumentBidAskPairContract>();
        }

        public void Start()
        {
            var settings = new RabbitMqSubscriptionSettings()
            {
                ConnectionString = _settings.MtRabbitMqConnString,
                ExchangeName = _settings.RabbitMqQueues.OrderbookPrices.ExchangeName,
                QueueName =
                    QueueHelper.BuildQueueName(_settings.RabbitMqQueues.OrderbookPrices.ExchangeName,
                        nameof(PricesCacheService)),
                IsDurable = false
            };

            _subscriber =
                new RabbitMqSubscriber<InstrumentBidAskPairContract>(settings, new DefaultErrorHandlingStrategy(_log, settings))
                    .SetMessageDeserializer(new FrontEndDeserializer<InstrumentBidAskPairContract>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .SetLogger(_log)
                    .Subscribe(ProcessPrice)
                    .Start();
        }

        public InstrumentBidAskPairContract[] GetPrices()
        {
            lock (_lastPrices)
            {
                return _lastPrices.Values.ToArray();
            }
        }

        public void Dispose()
        {
            _subscriber.Stop();
        }

        private Task ProcessPrice(InstrumentBidAskPairContract instrumentBidAskPair)
        {
            lock (_lastPrices)
            {
                if (!_lastPrices.ContainsKey(instrumentBidAskPair.Id))
                {
                    _lastPrices.Add(instrumentBidAskPair.Id, instrumentBidAskPair);
                }
                else
                {
                    _lastPrices[instrumentBidAskPair.Id] = instrumentBidAskPair;
                }
            }

            return Task.FromResult(0);
        }
    }
}
