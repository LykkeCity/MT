// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqQueueInfoWithLogging : RabbitMqQueueInfo
    {
        [Optional]
        public bool LogEventPublishing { get; set; } = true;
    }
}