// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Common.RabbitMq;

namespace MarginTrading.Backend.Core.Settings
{
    public class RabbitMqPublishers
    {
        public RabbitMqPublisherInfoWithLogging OrderHistory { get; set; }
        public RabbitMqPublisherInfoWithOccasionalLogging OrderbookPrices { get; set; }
        public RabbitMqPublisherInfoWithLogging AccountMarginEvents { get; set; }
        public RabbitMqPublisherInfoWithLogging AccountStats { get; set; }
        public RabbitMqPublisherInfoWithLogging Trades { get; set; }
        public RabbitMqPublisherInfoWithLogging PositionHistory { get; set; }
        public RabbitMqPublisherInfo MarginTradingEnabledChanged { get; set; }
        public RabbitMqPublisherInfoWithLogging ExternalOrder { get; set; }
        
        public RabbitMqPublisherInfoWithLogging RfqChangedRabbitMqSettings { get; set; }
    }
}