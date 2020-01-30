// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MarginTrading.Backend.Core.Services
{
    /// <summary>
    /// Responsible for RabbitMQ queues validation.
    /// </summary>
    public interface IQueueValidationService
    {
        /// <summary>
        /// Throws <see cref="System.Exception"/> if one of more queues contains not delivered messages.
        /// </summary>
        /// <param name="withPoison">If <c>true</c> the poison queues will be checked.</param>
        void ThrowExceptionIfQueuesNotEmpty(bool withPoison = false);

        /// <summary>
        /// Returns count of messages for each queue.
        /// </summary>
        /// <param name="withPoison">If <c>true</c> the poison queues will be checked.</param>
        /// <returns></returns>
        IReadOnlyDictionary<string, uint> GetMessagesCount(bool withPoison = false);
    }
}