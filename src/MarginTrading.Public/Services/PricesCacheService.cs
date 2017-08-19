using System;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Common.RabbitMq;
using System.Collections.Generic;
using System.Linq;
using Lykke.RabbitMqBroker;
using MarginTrading.Core;
using MarginTrading.Public.Settings;
using Microsoft.Extensions.PlatformAbstractions;

namespace MarginTrading.Public.Services
{
    public interface IPricesCacheService
    {
        InstrumentBidAskPair[] GetPrices();
    }

    public class PricesCacheService : IPricesCacheService, IStartable, IDisposable
    {
        private readonly MtPublicBaseSettings _settings;
        private readonly ILog _log;
        private RabbitMqSubscriber<InstrumentBidAskPair> _subscriber;
        private readonly Dictionary<string, InstrumentBidAskPair> _lastPrices;

        public PricesCacheService(MtPublicBaseSettings settings,
            ILog log)
        {
            _settings = settings;
            _log = log;
            _lastPrices = new Dictionary<string, InstrumentBidAskPair>();
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
                new RabbitMqSubscriber<InstrumentBidAskPair>(settings, new DefaultErrorHandlingStrategy(_log, settings))
                    .SetMessageDeserializer(new FrontEndDeserializer<InstrumentBidAskPair>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .SetLogger(_log)
                    .Subscribe(ProcessPrice)
                    .Start();
        }

        public InstrumentBidAskPair[] GetPrices()
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

        private Task ProcessPrice(InstrumentBidAskPair instrumentBidAskPair)
        {
            lock (_lastPrices)
            {
                if (!_lastPrices.ContainsKey(instrumentBidAskPair.Instrument))
                {
                    _lastPrices.Add(instrumentBidAskPair.Instrument, instrumentBidAskPair);
                }
                else
                {
                    _lastPrices[instrumentBidAskPair.Instrument] = instrumentBidAskPair;
                }
            }

            return Task.FromResult(0);
        }
    }
}
