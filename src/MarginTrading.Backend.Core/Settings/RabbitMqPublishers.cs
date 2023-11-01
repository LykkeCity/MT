// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Common.RabbitMq;

namespace MarginTrading.Backend.Core.Settings
{
    public class RabbitMqPublishers
    {
        public RabbitMqPublisherConfigurationWithLogging OrderHistory { get; set; }
        public RabbitMqPublisherConfigurationWithOccasionalLogging OrderbookPrices { get; set; }
        public RabbitMqPublisherConfigurationWithLogging AccountMarginEvents { get; set; }
        public RabbitMqPublisherConfigurationWithLogging AccountStats { get; set; }
        public RabbitMqPublisherConfigurationWithLogging Trades { get; set; }
        public RabbitMqPublisherConfigurationWithLogging PositionHistory { get; set; }
        public RabbitMqPublisherConfiguration MarginTradingEnabledChanged { get; set; }
        public RabbitMqPublisherConfigurationWithLogging ExternalOrder { get; set; }
        public RabbitMqPublisherConfigurationWithLogging RfqChanged { get; set; }
    }
}