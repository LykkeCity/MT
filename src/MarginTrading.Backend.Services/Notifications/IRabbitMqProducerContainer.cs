// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.RabbitMqBroker.Publisher;
using MarginTrading.Common.RabbitMq;

namespace MarginTrading.Backend.Services.Notifications
{
    public interface IRabbitMqProducerContainer
    {
        /// <summary>
        /// Registers a producer with RabbitMqPublisherInfoWithLogging publisherInfo.
        /// Human-readable message logging is enabled based on a setting in publisherInfo.
        /// </summary>
        /// <param name="publisherInfo"></param>
        /// <typeparam name="TMessage"></typeparam>
        void RegisterProducer<TMessage>(RabbitMqPublisherInfoWithLogging publisherInfo);
        
        /// <summary>
        /// Registers a producer with RabbitMqPublisherInfo publisherInfo.
        /// Human-readable message logging is disabled.
        /// </summary>
        /// <param name="publisherInfo"></param>
        /// <typeparam name="TMessage"></typeparam>
        void RegisterProducer<TMessage>(RabbitMqPublisherInfo publisherInfo);

        /// <summary>
        /// Gets producer for a specified type.
        /// PublisherInfo contains LogEventPublishing flag that states if a message should be logged in plain json.
        /// </summary>
        /// <typeparam name="TMessage">Type of produced message</typeparam>
        /// <returns></returns>
        (RabbitMqPublisherInfoWithLogging PublisherInfo, IMessageProducer<TMessage> Producer) GetProducer<TMessage>();
    }
}