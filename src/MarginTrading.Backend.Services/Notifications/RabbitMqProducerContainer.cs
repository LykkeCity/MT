// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Lykke.RabbitMqBroker.Publisher;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.RabbitMq;

namespace MarginTrading.Backend.Services.Notifications
{
    public class RabbitMqProducerContainer : IRabbitMqProducerContainer
    {
        private readonly IRabbitMqService _rabbitMqService;
        private readonly MarginTradingSettings _settings;

        private readonly Dictionary<Type, object> _producers = new Dictionary<Type, object>();

        private readonly Dictionary<Type, RabbitMqQueueInfoWithLogging> _producerSettings =
            new Dictionary<Type, RabbitMqQueueInfoWithLogging>();

        public RabbitMqProducerContainer(IRabbitMqService rabbitMqService,
            MarginTradingSettings settings)
        {
            _rabbitMqService = rabbitMqService;
            _settings = settings;
        }

        /// <inheritdoc />
        public void RegisterProducer<TMessage>(RabbitMqQueueInfoWithLogging queueInfo)
        {
            RegisterProducerImpl<TMessage>(queueInfo);
        }

        /// <inheritdoc />
        public void RegisterProducer<TMessage>(RabbitMqQueueInfo queueInfo)
        {
            RegisterProducerImpl<TMessage>(queueInfo.WithLogging(false));
        }

        /// <inheritdoc />
        public void RegisterProducer<TMessage>(RabbitMqSettings settings, bool shouldLogProducedEvents)
        {
            var producer = _rabbitMqService.GetProducer(settings, _rabbitMqService.GetJsonSerializer<TMessage>());
            var type = typeof(TMessage);
            _producers.Add(type, producer);
            _producerSettings.Add(type, new RabbitMqQueueInfoWithLogging()
            {
                ExchangeName = settings.ExchangeName,
                LogEventPublishing = shouldLogProducedEvents,
            });
        }

        private void RegisterProducerImpl<TMessage>(RabbitMqQueueInfoWithLogging queueInfo)
        {
            var settings = queueInfo.ToRabbitMqSettings(_settings.MtRabbitMqConnString);
            var producer = _rabbitMqService.GetProducer(settings, _rabbitMqService.GetJsonSerializer<TMessage>());
            var type = typeof(TMessage);
            _producers.Add(type, producer);
            _producerSettings.Add(type, queueInfo);
        }


        /// <inheritdoc />
        public (RabbitMqQueueInfoWithLogging QueueInfo, IMessageProducer<TMessage> Producer) GetProducer<TMessage>()
        {
            var type = typeof(TMessage);
            return (
                _producerSettings[type],
                _producers[typeof(TMessage)] as IMessageProducer<TMessage>
            );
        }
    }
}