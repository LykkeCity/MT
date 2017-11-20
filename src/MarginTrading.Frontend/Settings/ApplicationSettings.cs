using Lykke.SettingsReader.Attributes;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Settings;

namespace MarginTrading.Frontend.Settings
{
    public class ApplicationSettings
    {
        public MtFrontendSettings MtFrontend {get; set;}
        public SlackNotificationSettings SlackNotifications { get; set; }
    }

    public class MtFrontendSettings
    {
        public MtSettings MarginTradingLive { get; set; }
        public MtSettings MarginTradingDemo { get; set; }
        public MtFrontSettings MarginTradingFront { get; set; }
    }

    public class MtSettings
    {
        public string ApiRootUrl { get; set; }
        public string ApiKey { get; set; }

        public string MtRabbitMqConnString { get; set; }
    }

    public class DbSettings
    {
        public string LogsConnString { get; set; }
        public string MarginTradingConnString { get; set; }
        public string ClientPersonalInfoConnString { get; set; }
    }

    public class MtQueues
    {
        public RabbitMqQueueInfo AccountChanged { get; set; }
        public RabbitMqQueueInfo OrderChanged { get; set; }
        public RabbitMqQueueInfo AccountStopout { get; set; }
        public RabbitMqQueueInfo UserUpdates { get; set; }
        public RabbitMqQueueInfo OrderbookPrices { get; set; }
        public RabbitMqQueueInfo Trades { get; set; }
    }

    public class MtFrontSettings
    {
        public string SessionServiceApiUrl { get; set; }
        public string DemoAccountIdPrefix { get; set; }
        public string[] AllowOrigins { get; set; }
        public DataReaderApiSettings DataReaderApiSettings { get; set; }

        #region From env variables

        [Optional]
        public string Env { get; set; }

        #endregion

        public DbSettings Db { get; set; }
        public MtQueues RabbitMqQueues { get; set; }
        public RequestLoggerSettings RequestLoggerSettings { get; set; }
        [Optional]
        public string ApplicationInsightsKey { get; set; }
    }

    public class DataReaderApiSettings
    {
        public string DemoApiUrl { get; set; }
        public string LiveApiUrl { get; set; }
        public string DemoApiKey { get; set; }
        public string LiveApiKey { get; set; }
    }
}
