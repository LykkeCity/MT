// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.RabbitMqBroker.Subscriber;
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

        public RabbitMqSettings BrokerSettingsRabbitMqSettings { get; set; }

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
        
        public ExchangeConnectorType ExchangeConnector { get; set; }
        
        public bool WriteOperationLog { get; set; }
        
        public SpecialLiquidationSettings SpecialLiquidation { get; set; }
        
        [Optional, CanBeNull]
        public ChaosSettings ChaosKitty { get; set; }

        [Optional] 
        public ThrottlingSettings Throttling { get; set; } = new ThrottlingSettings();

        [Optional]
        public bool UseSerilog { get; set; }

        [Optional] 
        public OvernightMarginSettings OvernightMargin { get; set; } = new OvernightMarginSettings();
        
        [Optional]
        public string DefaultExternalExchangeId { get; set; }

        [Optional]
        public int PendingOrderRetriesThreshold { get; set; } = 100;

        [Optional]
        public int SnapshotInsertTimeoutSec { get; set; } = 3600;

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public RedisSettings RedisSettings { get; set; }

        [Optional]
        public TimeSpan DeduplicationLockExtensionPeriod { get; set; } = TimeSpan.FromSeconds(1);

        [Optional]
        public TimeSpan DeduplicationLockExpiryPeriod { get; set; } = TimeSpan.FromSeconds(10);
        
        public StartupQueuesCheckerSettings StartupQueuesChecker { get; set; }
        
        [Optional]
        public TimeSpan GavelTimeout { get; set; } = TimeSpan.FromSeconds(3);
        
        [Optional]
        public OrderbookValidationSettings OrderbookValidation { get; set; } = new OrderbookValidationSettings();

        [Optional]
        public DefaultTradingConditionsSettings DefaultTradingConditionsSettings { get; set; } = new DefaultTradingConditionsSettings();

        public DefaultLegalEntitySettings DefaultLegalEntitySettings { get; set; }

        public string BrokerId { get; set; }
    }
}