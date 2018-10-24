using JetBrains.Annotations;
using Lykke.MarginTrading.BrokerBase.Models;
using Lykke.MarginTrading.BrokerBase.Settings;
using Lykke.SettingsReader.Attributes;
using MarginTrading.Common.RabbitMq;

namespace MarginTrading.AccountMarginEventsBroker
{
    [UsedImplicitly]
    public class Settings : BrokerSettingsBase
    {
        public Db Db { get; set; }
        public RabbitMqQueues RabbitMqQueues { get; set; }
    }
    
    [UsedImplicitly]
    public class Db : BrokersLogsSettings
    {
        public string ConnString { get; set; }
    }
    
    [UsedImplicitly]
    public class RabbitMqQueues
    {
        public RabbitMqQueueInfo AccountMarginEvents { get; set; }
    }
}