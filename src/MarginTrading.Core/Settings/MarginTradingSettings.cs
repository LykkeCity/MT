﻿using System;
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
        public string ApiRootUrl { get; set; }
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
        public RabbitMqSettings SpotRabbitMqSettings { get; set; }
        public string MtRabbitMqConnString { get; set; }
        public string[] BaseAccountAssets { get; set; }
        [Optional]
        public AccountAssetsSettings DefaultAccountAssetsSettings { get; set; }
        public RequestLoggerSettings RequestLoggerSettings { get; set; }
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
        public string SharedStorageConnString { get; set; }
        public string HistoryConnString { get; set; }
        public string StateConnString { get; set; }
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
        public double SwapLong { get; set; }

        [Optional]
        public double SwapShort { get; set; }

        [Optional]
        public double SwapLongPct { get; set; }

        [Optional]
        public double SwapShortPct { get; set; }

        [Optional]
        public double CommissionLong { get; set; }

        [Optional]
        public double CommissionShort { get; set; }

        [Optional]
        public double CommissionLot { get; set; }

        [Optional]
        public double DeltaBid { get; set; }

        [Optional]
        public double DeltaAsk { get; set; }

        [Optional]
        public double DealLimit { get; set; }

        [Optional]
        public double PositionLimit { get; set; }
    }
}
