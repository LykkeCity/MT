// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Common.Services;

namespace MarginTrading.Common.RabbitMq
{
    public sealed class RabbitMqPublisherInfoWithOccasionalLogging : RabbitMqPublisherInfoWithLogging
    {
        public override IRabbitMqPublisherLoggingStrategy LoggingStrategy { get; } =
            new OneInAThousandLoggingStrategy();
    }
}