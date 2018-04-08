using MarginTrading.BrokerBase.Settings;
using MarginTrading.Common.RabbitMq;

namespace MarginTrading.MigrateApp
{
    public class Settings : BrokerSettingsBase
    {
        public Db Db { get; set; }
        public RabbitMqQueues RabbitMqQueues { get; set; }
    }
    
    public class Db
    {
        public string HistoryConnString { get; set; }
        public string MarginTradingConnString { get; set; }
        public string ReportsConnString { get; set; }
    }
    
    public class RabbitMqQueues
    {
        public RabbitMqQueueInfo OrderbookPrices { get; set; }
    }
}