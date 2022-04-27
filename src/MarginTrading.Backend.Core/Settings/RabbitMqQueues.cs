// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Common.RabbitMq;

namespace MarginTrading.Backend.Core.Settings
{
    public class RabbitMqQueues
    {
        public RabbitMqQueueInfoWithLogging OrderHistory { get; set; }
        public RabbitMqQueueInfo OrderbookPrices { get; set; }
        public RabbitMqQueueInfoWithLogging AccountMarginEvents { get; set; }
        public RabbitMqQueueInfoWithLogging AccountStats { get; set; }
        public RabbitMqQueueInfoWithLogging Trades { get; set; }
        public RabbitMqQueueInfoWithLogging PositionHistory { get; set; }
        public RabbitMqQueueInfo MarginTradingEnabledChanged { get; set; }
        public RabbitMqQueueInfoWithLogging ExternalOrder { get; set; }
        public RabbitMqQueueInfo SettingsChanged { get; set; }
    }
}