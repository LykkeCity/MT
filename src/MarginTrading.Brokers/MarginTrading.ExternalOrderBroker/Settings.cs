using MarginTrading.BrokerBase.Settings;
using MarginTrading.Common.RabbitMq;

namespace MarginTrading.ExternalOrderBroker
{
    public class Settings : BrokerSettingsBase
    {
        public Db Db { get; set; }
        public RabbitMqQueues RabbitMqQueues { get; set; }
    }
    
    public class Db
    {
        public string ReportsConnString { get; set; }
        public string ReportsSqlConnString { get; set; }
    }
    
    public class RabbitMqQueues
    {
        public RabbitMqQueueInfo ExternalOrder { get; set; }
    }
}