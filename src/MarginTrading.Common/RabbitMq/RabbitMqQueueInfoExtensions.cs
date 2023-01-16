// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Common.RabbitMq
{
    public static class RabbitMqQueueInfoExtensions
    {
        public static RabbitMqQueueInfoWithLogging WithLogging(this RabbitMqQueueInfo info, bool logEventPublishing)
        {
            return new RabbitMqQueueInfoWithLogging()
            {
                ExchangeName = info.ExchangeName,
                LogEventPublishing = logEventPublishing,
            };
        }
    }
}