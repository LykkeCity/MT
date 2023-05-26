// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Common.Services
{
    public sealed class AlwaysOnLoggingStrategy : IRabbitMqPublisherLoggingStrategy
    {
        public bool CanLog() => true;
    }
}