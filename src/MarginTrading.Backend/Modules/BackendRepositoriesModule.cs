using Autofac;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Logs;
using MarginTrading.AzureRepositories;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Monitoring;
using MarginTrading.Core.Notifications;
using MarginTrading.Core.Settings;
using MarginTrading.Services;
using Microsoft.Extensions.PlatformAbstractions;

namespace MarginTrading.Backend.Modules
{
    public class BackendRepositoriesModule : Module
    {
        private readonly MarginSettings _settings;

        public BackendRepositoriesModule(MarginSettings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            LykkeLogToAzureStorage log = new LykkeLogToAzureStorage(PlatformServices.Default.Application.ApplicationName, 
                new AzureTableStorage<LogEntity>(_settings.Db.LogsConnString, "MarginTradingBackendLog", null));

            builder.RegisterInstance((ILog)log)
                .As<ILog>()
                .SingleInstance();

            builder.Register<IMarginTradingOperationsLogRepository>(ctx =>
                new MarginTradingOperationsLogRepository(new AzureTableStorage<OperationLogEntity>(_settings.Db.LogsConnString, "MarginTradingBackendOperationsLog", log))
            ).SingleInstance();

            builder.Register<IClientSettingsRepository>(ctx =>
                AzureRepoFactories.Clients.CreateTraderSettingsRepository(_settings.Db.ClientPersonalInfoConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingAccountsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountsRepository(_settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingOrdersHistoryRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateOrdersHistoryRepository(_settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingAccountHistoryRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountHistoryRepository(_settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IMatchingEngineRoutesRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateMatchingEngineRoutesRepository(_settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingConditionRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateTradingConditionsRepository(_settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingAccountGroupRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountGroupRepository(_settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingAccountAssetRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountAssetsRepository(_settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingAssetsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAssetsRepository(_settings.Db.DictsConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingBlobRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateBlobRepository(_settings.Db.MarginTradingConnString)
            ).SingleInstance();

            builder.Register<IServiceMonitoringRepository>(ctx =>
                AzureRepoFactories.Monitoring.CreateServiceMonitoringRepository(_settings.Db.SharedStorageConnString, log)
            ).SingleInstance();

            builder.Register<ISlackNotificationsProducer>(ctx =>
                AzureRepoFactories.Notifications.CreateSlackNotificationsProducer(_settings.Db.SharedStorageConnString)
            ).SingleInstance();

            builder.Register<IAppGlobalSettingsRepositry>(ctx =>
                AzureRepoFactories.Settings.CreateAppGlobalSettingsRepository(_settings.Db.ClientPersonalInfoConnString, log)
            ).SingleInstance();

            builder.RegisterType<MatchingEngineInMemoryRepository>()
                .As<IMatchingEngineRepository>()
                .SingleInstance();

            builder.Register<IMarginTradingTransactionRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateTransactionRepository(_settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingPositionRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreatePositionRepository(_settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingTradingOrderRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateTradingOrderRepository(_settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

            builder.Register<IElementaryTransactionsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateElementaryTransactionsRepository(_settings.Db.MarginTradingConnString, log)
            ).SingleInstance();

			builder.Register<ISampleQuoteCacheRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateSampleQuoteCacheRepository(_settings.Db.MarginTradingConnString, log)
			).SingleInstance();

			builder.Register<IQuoteHistoryRepository>(ctx =>
				AzureRepoFactories.CreateQuoteHistoryRepository(_settings.Db.MarginTradingConnString, log)
			).SingleInstance();

			builder.Register<IMarginTradingIndividualValuesAtRiskRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateIndividualValuesAtRiskRepository(_settings.Db.MarginTradingConnString, log)
			).SingleInstance();

			builder.Register<IMarginTradingAggregateValuesAtRiskRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateAggregateValuesAtRiskRepository(_settings.Db.MarginTradingConnString, log)
			).SingleInstance();

		}
	}
}
