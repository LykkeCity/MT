using MarginTrading.BrokerBase.Settings;
using MarginTrading.Common.RabbitMq;

namespace MarginTrading.ExternalOrderBroker.Settings
{
    public class AppSettings : BrokerSettingsBase
    {
        public DbSettings Db { get; set; }
        public RabbitMqQueuesSettings RabbitMqQueues { get; set; }
    }
}