// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Common.RabbitMq;

namespace MarginTrading.Backend.Services.Infrastructure
{
    /// <inheritdoc/> 
    public class QueueValidationService : IQueueValidationService
    {
        private const string PoisonPostfix = "-poison";

        private readonly string _connectionString;
        private readonly IReadOnlyList<string> _queueNames;

        /// <summary>
        /// Initializes an new instance of <see cref="QueueValidationService"/>.
        /// </summary>
        /// <param name="connectionString">The RabbitMQ connection string.</param>
        /// <param name="queueNames">A collection of queues to be checked.</param>
        public QueueValidationService(string connectionString, IReadOnlyList<string> queueNames)
        {
            _connectionString = connectionString;
            _queueNames = queueNames;
        }

        /// <inheritdoc/>
        public void ThrowExceptionIfQueuesNotEmpty(bool withPoison = false)
        {
            var messagesCount = GetMessagesCount(withPoison);

            foreach (var (queueName, messageCount) in messagesCount)
            {
                if (messageCount > 0)
                {
                    throw new Exception(
                        $"One or more queues contains not delivered messages. Currently [{queueName}] contains [{messageCount}] messages.");
                }
            }
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, uint> GetMessagesCount(bool withPoison = false)
        {
            var result = new Dictionary<string, uint>();

            foreach (var queueName in _queueNames)
            {
                var messageCount = RabbitMqService.GetMessageCount(_connectionString, queueName);
                result.Add(queueName, messageCount);

                if (withPoison)
                {
                    var poisonQueueName = $"{queueName}{PoisonPostfix}";
                    var poisonMessageCount = RabbitMqService.GetMessageCount(_connectionString, poisonQueueName);
                    result.Add(poisonQueueName, poisonMessageCount);
                }
            }

            return result;
        }
    }
}