﻿using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;
using MarginTrading.Common.Settings;
using RabbitMqSettings = MarginTrading.Common.RabbitMq.RabbitMqSettings;

namespace MarginTrading.Backend.Core.Settings
{
    public class MarginTradingSettings
    {
        
        #region from Env variables

        [Optional]
        public string Env { get; set; }

        [Optional]
        public bool IsLive { get; set; }

        #endregion
        
        public string ApiKey { get; set; }
        
        public Db Db { get; set; }
        
        public RabbitMqQueues RabbitMqQueues { get; set; }
        
        [Optional, CanBeNull]
        public RabbitMqSettings MarketMakerRabbitMqSettings { get; set; }
        
        [Optional, CanBeNull]
        public RabbitMqSettings StpAggregatorRabbitMqSettings { get; set; }
        
        [Optional, CanBeNull] 
        public RabbitMqSettings FxRateRabbitMqSettings { get; set; } 
        
        [Optional, CanBeNull]
        public RabbitMqSettings RisksRabbitMqSettings { get; set; }
        
        [AmqpCheck]
        public string MtRabbitMqConnString { get; set; }
        
        public RequestLoggerSettings RequestLoggerSettings { get; set; }
        
        [Optional]
        public virtual TelemetrySettings Telemetry { get; set; }
        
        [Optional]
        public int MaxMarketMakerLimitOrderAge { get; set; }
        
        public ReportingEquivalentPricesSettings[] ReportingEquivalentPricesSettings { get; set; }
        
        [Optional]
        public bool UseDbIdentityGenerator { get; set; }
        
        public BlobPersistenceSettings BlobPersistence { get; set; } 

        public CqrsSettings Cqrs { get; set; }
    }
}