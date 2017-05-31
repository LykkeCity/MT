using Lykke.SettingsReader.Attributes;
using MarginTrading.Core.Settings;

namespace MarginTrading.Public.Settings
{
    public class ApplicationSettings
    {
        public MtPublicBaseSettings MtPublic { get; set; }
    }

    public class MtPublicBaseSettings
    {
        [Optional]
        public string Env { get; set; }
        public string WampPricesTopicName { get; set; }
        public string MtRabbitMqConnString { get; set; }

        public DbSettings Db { get; set; }
        public MtQueues RabbitMqQueues { get; set; }
    }

    public class DbSettings
    {
        public string LogsConnString { get; set; }
    }

    public class MtQueues
    {
        public RabbitMqQueueInfo OrderbookPrices { get; set; }
    }
}
