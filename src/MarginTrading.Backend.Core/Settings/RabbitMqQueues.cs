using MarginTrading.Common.RabbitMq;

namespace MarginTrading.Backend.Core.Settings
{
    public class RabbitMqQueues
    {
        public RabbitMqQueueInfo AccountHistory { get; set; }
        public RabbitMqQueueInfo OrderHistory { get; set; }
        public RabbitMqQueueInfo OrderRejected { get; set; }
        public RabbitMqQueueInfo OrderbookPrices { get; set; }
        public RabbitMqQueueInfo OrderChanged { get; set; }
        public RabbitMqQueueInfo AccountChanged { get; set; }
        public RabbitMqQueueInfo AccountStopout { get; set; }
        public RabbitMqQueueInfo UserUpdates { get; set; }
        public RabbitMqQueueInfo AccountMarginEvents { get; set; }
        public RabbitMqQueueInfo AccountStats { get; set; }
        public RabbitMqQueueInfo Trades { get; set; }
        public RabbitMqQueueInfo MarginTradingEnabledChanged { get; set; }
    }
}