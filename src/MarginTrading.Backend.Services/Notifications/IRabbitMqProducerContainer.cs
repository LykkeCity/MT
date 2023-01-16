// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.RabbitMqBroker.Publisher;
using MarginTrading.Common.RabbitMq;

namespace MarginTrading.Backend.Services.Notifications
{
    public interface IRabbitMqProducerContainer
    {
        /// <summary>
        /// Registers a producer with RabbitMqQueueInfoWithLogging queueInfo.
        /// Human-readable message logging is enabled based on a setting in queueInfo.
        /// </summary>
        /// <param name="queueInfo"></param>
        /// <typeparam name="TMessage"></typeparam>
        void RegisterProducer<TMessage>(RabbitMqQueueInfoWithLogging queueInfo);
        
        /// <summary>
        /// Registers a producer with RabbitMqQueueInfo queueInfo.
        /// Human-readable message logging is disabled.
        /// </summary>
        /// <param name="queueInfo"></param>
        /// <typeparam name="TMessage"></typeparam>
        void RegisterProducer<TMessage>(RabbitMqQueueInfo queueInfo);

        /// <summary>
        /// Registers a producer with extended settings from RabbitMqSettings.
        /// Human-readable message logging is enabled based on the shouldLogProducedEvents flag.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="shouldLogProducedEvents"></param>
        /// <typeparam name="TMessage"></typeparam>
        void RegisterProducer<TMessage>(RabbitMqSettings settings, bool shouldLogProducedEvents);

        /// <summary>
        /// Gets producer for a specified type.
        /// QueueInfo contains LogEventPublishing flag that states if a message should be logged in plain json.
        /// </summary>
        /// <typeparam name="TMessage">Type of produced message</typeparam>
        /// <returns></returns>
        (RabbitMqQueueInfoWithLogging QueueInfo, IMessageProducer<TMessage> Producer) GetProducer<TMessage>();
    }
}