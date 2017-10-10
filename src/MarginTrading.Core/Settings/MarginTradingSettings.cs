using System;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Core.Settings
{
    public class MarginTradingSettings
    {
        public MarginSettings MarginTradingLive { get; set; }
        public MarginSettings MarginTradingDemo { get; set; }
    }

    public class MarginSettings
    {
        public string MetricLoggerLine { get; set; }
        public string ApiKey { get; set; }
        public string DemoAccountIdPrefix { get; set; }
        public bool RemoteConsoleEnabled { get; set; }

        #region from Env variables

        [Optional]
        public string Env { get; set; }

        [Optional]
        public bool IsLive { get; set; }

        #endregion

        public Db Db { get; set; }
        public RabbitMqQueues RabbitMqQueues { get; set; }
        public RabbitMqSettings MarketMakerRabbitMqSettings { get; set; }
        public string MtRabbitMqConnString { get; set; }
        public string[] BaseAccountAssets { get; set; }
        [Optional]
        public AccountAssetsSettings DefaultAccountAssetsSettings { get; set; }
        public RequestLoggerSettings RequestLoggerSettings { get; set; }
        [Optional]
        public string ApplicationInsightsKey { get; set; }
        [Optional]
        public virtual TelemetrySettings Telemetry { get; set; }
    }

    public class NotificationSettings
    {
        public string NotificationsHubName { get; set; }
        public string NotificationsHubConnectionString { get; set; }
    }

    public class EmailSenderSettings
    {
        public string ServiceUrl { get; set; }
    }

    public class Db
    {
        public string LogsConnString { get; set; }
        public string MarginTradingConnString { get; set; }
        public string ClientPersonalInfoConnString { get; set; }
        public string DictsConnString { get; set; }
        public string HistoryConnString { get; set; }
        public string StateConnString { get; set; }
        public string ReportsConnString { get; set; }
    }

    public class RabbitMqQueues
    {
        public RabbitMqQueueInfo AccountHistory { get; set; }
        public RabbitMqQueueInfo OrderHistory { get; set; }
        public RabbitMqQueueInfo OrderRejected { get; set; }
        public RabbitMqQueueInfo OrderbookPrices { get; set; }
        public RabbitMqQueueInfo OrderChanged { get; set; }
        public RabbitMqQueueInfo AccountChanged { get; set; }
        public RabbitMqQueueInfo AccountStopout { get; set; }
        public RabbitMqQueueInfo UserUpdates { get; set; }
        public RabbitMqQueueInfo AccountMarginEvents { get; set; }
    }

    public class RabbitMqQueueInfo
    {
        public string ExchangeName { get; set; }
    }

    public class RabbitMqSettings
    {
        public string ConnectionString { get; set; }
        public string ExchangeName { get; set; }
        public bool IsDurable { get; set; }
    }

    public class MarketMakerSettings
    {
        public string DayOffStartDay { get; set; }
        public int DayOffStartHour { get; set; }
        public string DayOffEndDay { get; set; }
        public int DayOffEndHour { get; set; }
        public string[] AssetsWithoutDayOff { get; set; }
    }

    public class AccountAssetsSettings
    {
        [Optional]
        public int LeverageInit { get; set; }

        [Optional]
        public int LeverageMaintenance { get; set; }

        [Optional]
        public decimal SwapLong { get; set; }

        [Optional]
        public decimal SwapShort { get; set; }

        [Optional]
        public decimal SwapLongPct { get; set; }

        [Optional]
        public decimal SwapShortPct { get; set; }

        [Optional]
        public decimal CommissionLong { get; set; }

        [Optional]
        public decimal CommissionShort { get; set; }

        [Optional]
        public decimal CommissionLot { get; set; }

        [Optional]
        public decimal DeltaBid { get; set; }

        [Optional]
        public decimal DeltaAsk { get; set; }

        [Optional]
        public decimal DealLimit { get; set; }

        [Optional]
        public decimal PositionLimit { get; set; }
    }

    /// <summary>
    /// Telementry settings
    /// </summary>
    public class TelemetrySettings
    {
        /// <summary>
        /// Minimal duration of lock in ms to send event to telemetry
        /// </summary>
        public int LockMetricThreshold { get; set; }
    }
}
