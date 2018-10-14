using Lykke.MarginTrading.BrokerBase.Models;
using Lykke.MarginTrading.BrokerBase.Settings;
using MarginTrading.Common.RabbitMq;

namespace MarginTrading.AccountMarginEventsBroker
{
    public class Settings : BrokerSettingsBase
    {
        public Db Db { get; set; }
        public RabbitMqQueues RabbitMqQueues { get; set; }
    }
    
    public class Db
    {
        public StorageMode StorageMode { get; set; }
        public string ConnString { get; set; }
    }
    
    public class RabbitMqQueues
    {
        public RabbitMqQueueInfo AccountMarginEvents { get; set; }
    }
}