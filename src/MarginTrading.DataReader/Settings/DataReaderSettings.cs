using Lykke.SettingsReader.Attributes;

namespace MarginTrading.DataReader.Settings
{
    public class DataReaderSettings
    {
        public string ApiKey { get; set; }

        [Optional]
        public string Env { get; set; }

        [Optional]
        public bool IsLive { get; set; }

        public Db Db { get; set; }

        [Optional]
        public string ApplicationInsightsKey { get; set; }

        public RabbitMqConsumers Consumers { get; set; }
    }
}