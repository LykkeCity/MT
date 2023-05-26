// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.SettingsReader.Attributes;
using MarginTrading.Common.Services;

namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqPublisherInfoWithLogging : RabbitMqPublisherInfo
    {
        [Optional]
        public bool LogEventPublishing { get; } = true;

        public virtual IRabbitMqPublisherLoggingStrategy LoggingStrategy { get; } = new AlwaysOnLoggingStrategy();
    }
}