using MarginTrading.BrokerBase.Settings;
using MarginTrading.Common.RabbitMq;

namespace MarginTrading.AccountReportsBroker
{
    public class Settings : BrokerSettingsBase
    {
        public Db Db { get; set; }
        public RabbitMqQueues RabbitMqQueues { get; set; }
    }
    
    public class Db
    {
        public string ReportsConnString { get; set; }
        public string HistoryConnString { get; set; }
    }
    
    public class RabbitMqQueues
    {
        public RabbitMqQueueInfo AccountStats { get; set; }
        public RabbitMqQueueInfo AccountChanged { get; set; }
    }
}