using MarginTrading.Common.RabbitMq;

namespace MarginTrading.Backend.Core.Settings
{
    public class RabbitMqQueues
    {
        public RabbitMqQueueInfo OrderHistory { get; set; }
        public RabbitMqQueueInfo OrderbookPrices { get; set; }
        public RabbitMqQueueInfo AccountChanged { get; set; }
        public RabbitMqQueueInfo AccountMarginEvents { get; set; }
        public RabbitMqQueueInfo AccountStats { get; set; }
        public RabbitMqQueueInfo Trades { get; set; }
        public RabbitMqQueueInfo PositionHistory { get; set; }
        public RabbitMqQueueInfo MarginTradingEnabledChanged { get; set; }
        public RabbitMqQueueInfo ExternalOrder { get; set; }
        public RabbitMqQueueInfo SettingsChanged { get; set; }
    }
}