// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqQueueInfoWithLogging : RabbitMqQueueInfo
    {
        public bool LogEventPublishing { get; set; } = true;
    }
}