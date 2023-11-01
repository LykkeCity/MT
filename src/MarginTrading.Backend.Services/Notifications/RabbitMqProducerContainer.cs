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

        private readonly Dictionary<Type, RabbitMqPublisherConfigurationWithLogging> _producerSettings =
            new Dictionary<Type, RabbitMqPublisherConfigurationWithLogging>();

        public RabbitMqProducerContainer(IRabbitMqService rabbitMqService,
            MarginTradingSettings settings)
        {
            _rabbitMqService = rabbitMqService;
            _settings = settings;
        }

        /// <inheritdoc />
        public void RegisterProducer<TMessage>(RabbitMqPublisherConfigurationWithLogging publisherConfig)
        {
            RegisterProducerImpl<TMessage>(publisherConfig);
        }

        /// <inheritdoc />
        public void RegisterProducer<TMessage>(RabbitMqPublisherConfiguration publisherConfig)
        {
            RegisterProducerImpl<TMessage>(publisherConfig.ToPublisherConfigWithLogging(false));
        }

        private void RegisterProducerImpl<TMessage>(RabbitMqPublisherConfigurationWithLogging publisherInfo)
        {
            var producer = _rabbitMqService.GetProducer(publisherInfo, _rabbitMqService.GetJsonSerializer<TMessage>());
            var type = typeof(TMessage);
            _producers.Add(type, producer);
            _producerSettings.Add(type, publisherInfo);
        }


        /// <inheritdoc />
        public (RabbitMqPublisherConfigurationWithLogging PublisherConfig, IMessageProducer<TMessage> Producer)
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