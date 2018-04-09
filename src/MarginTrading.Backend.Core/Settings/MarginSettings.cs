using System;
using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Settings;

namespace MarginTrading.Backend.Core.Settings
{
    public class MarginSettings
    {
        public string ApiKey { get; set; }
        public string DemoAccountIdPrefix { get; set; }

        #region from Env variables

        [Optional]
        public string Env { get; set; }

        [Optional]
        public bool IsLive { get; set; }

        #endregion

        public Db Db { get; set; }
        
        public RabbitMqQueues RabbitMqQueues { get; set; }
        
        public RabbitMqSettings MarketMakerRabbitMqSettings { get; set; }
        
        [Optional]
        public RabbitMqSettings StpAggregatorRabbitMqSettings { get; set; }
        
        [Optional, CanBeNull]
        public RabbitMqSettings RisksRabbitMqSettings { get; set; }
        
        [AmqpCheck]
        public string MtRabbitMqConnString { get; set; }
        
        public string[] BaseAccountAssets { get; set; } = new string[0];
        
        [Optional]
        public AccountAssetsSettings DefaultAccountAssetsSettings { get; set; }
        
        public RequestLoggerSettings RequestLoggerSettings { get; set; }
        
        [Optional]
        public string ApplicationInsightsKey { get; set; }
        
        [Optional]
        public virtual TelemetrySettings Telemetry { get; set; }
        
        public int MaxMarketMakerLimitOrderAge { get; set; }
        
        public ReportingEquivalentPricesSettings[] ReportingEquivalentPricesSettings { get; set; }
        
        public TimeSpan OvernightSwapCalculationTime { get; set; }
        public bool SendOvernightSwapEmails { get; set; }
    }
}