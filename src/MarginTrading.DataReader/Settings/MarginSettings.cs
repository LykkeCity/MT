using Lykke.SettingsReader.Attributes;

namespace MarginTrading.DataReader.Settings
{
    public class MarginSettings
    {
        public string ApiKey { get; set; }
        public string MetricLoggerLine { get; set; }

        [Optional]
        public string Env { get; set; }

        [Optional]
        public bool IsLive { get; set; }

        public Db Db { get; set; }

        [Optional]
        public string ApplicationInsightsKey { get; set; }
    }
}