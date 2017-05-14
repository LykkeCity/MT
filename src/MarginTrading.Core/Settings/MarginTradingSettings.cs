using System.Collections.Generic;

namespace MarginTrading.Core.Settings
{
    public class MtBackendSettings
    {
        public MarginTradingSettings MtBackend { get; set; }
    }

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
		public string ClientAccountServiceApiUrl { get; set; }


		#region from Env variables

		public string Env { get; set; }
        public bool IsLive { get; set; }
        public bool AutoCreateAccounts { get; set; }

        #endregion

        public NotificationSettings Notifications { get; set; }
        public EmailServiceBus EmailServiceBus { get; set; }
        public Db Db { get; set; }
        public RabbitMqQueues RabbitMqQueues { get; set; }
        public RabbitMqSettings RabbitMqSettings { get; set; }
        public MarginTradingRabbitMqSettings MarginTradingRabbitMqSettings { get; set; }
		public RiskManagementSettings RiskManagement { get; set; }
	}

	public class RiskManagementSettings
	{
		public int QuoteSamplingInterval { get; set; }
		public int QuoteSampleMaxCount { get; set; }
		public double SamplingFrequency { get; set; }
		public bool EnforceCalculation { get; set; }
		public Dictionary<string, Dictionary<string, double>> CorrelationMatrix { get; set; }
		public Dictionary<string, double> PVaRSoftLimits { get; set; }
		public Dictionary<string, double> PVaRHardLimits { get; set; }
	}

	public class NotificationSettings
    {
        public string HubName { get; set; }
        public string ConnString { get; set; }
    }

    public class EmailServiceBus
    {
        public string Key { get; set; }
        public string QueueName { get; set; }
        public string NamespaceUrl { get; set; }
        public string PolicyName { get; set; }
    }

    

    public class Db
    {
        public string LogsConnString { get; set; }
        public string MarginTradingConnString { get; set; }
        public string ClientPersonalInfoConnString { get; set; }
        public string DictsConnString { get; set; }
        public string SharedStorageConnString { get; set; }
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
        public RabbitMqQueueInfo Transaction { get; set; }
		public RabbitMqQueueInfo ElementaryTransaction { get; set; }
		public RabbitMqQueueInfo OrderReport { get; set; }
		public RabbitMqQueueInfo ValueAtRiskLimits { get; set; }
		public RabbitMqQueueInfo PositionUpdates { get; set; }
		public RabbitMqQueueInfo IndividualValuesAtRisk { get; set; }
		public RabbitMqQueueInfo AggregateValuesAtRisk { get; set; }
	}

	public class RabbitMqQueueInfo
    {
        public string QueueName { get; set; }
        public string RoutingKeyName { get; set; }
    }

    public class RabbitMqSettings
    {
        public string ConnectionString { get; set; }
        public string QueueName { get; set; }
        public string ExchangeName { get; set; }
    }

    public class MarginTradingRabbitMqSettings
    {
        public string ConnectionString { get; set; }
        public string InternalConnectionString { get; set; }
        public string ExchangeName { get; set; }
    }
}
