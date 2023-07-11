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

        private readonly Dictionary<Type, RabbitMqPublisherInfoWithLogging> _producerSettings =
            new Dictionary<Type, RabbitMqPublisherInfoWithLogging>();

        public RabbitMqProducerContainer(IRabbitMqService rabbitMqService,
            MarginTradingSettings settings)
        {
            _rabbitMqService = rabbitMqService;
            _settings = settings;
        }

        /// <inheritdoc />
        public void RegisterProducer<TMessage>(RabbitMqPublisherInfoWithLogging publisherInfo)
        {
            RegisterProducerImpl<TMessage>(publisherInfo);
        }

        /// <inheritdoc />
        public void RegisterProducer<TMessage>(RabbitMqPublisherInfo publisherInfo)
        {
            RegisterProducerImpl<TMessage>(publisherInfo.WithLogging(false));
        }

        private void RegisterProducerImpl<TMessage>(RabbitMqPublisherInfoWithLogging publisherInfo)
        {
            var settings = publisherInfo.ToRabbitMqSettings(_settings.MtRabbitMqConnString);
            var producer = _rabbitMqService.GetProducer(settings, _rabbitMqService.GetJsonSerializer<TMessage>());
            var type = typeof(TMessage);
            _producers.Add(type, producer);
            _producerSettings.Add(type, publisherInfo);
        }


        /// <inheritdoc />
        public (RabbitMqPublisherInfoWithLogging PublisherInfo, IMessageProducer<TMessage> Producer)
            GetProducer<TMessage>()
        {
            var type = typeof(TMessage);
            return (
                _producerSettings[type],
                _producers[type] as IMessageProducer<TMessage>
            );
        }
    }
}