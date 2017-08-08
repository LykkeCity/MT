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
		private readonly ILog _log;

		public BackendRepositoriesModule(MarginSettings settings, ILog log)
		{
			_settings = settings;
			_log = log;
		}

		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterInstance(_log)
				.As<ILog>()
				.SingleInstance();

			builder.Register<IMarginTradingOperationsLogRepository>(ctx =>
				new MarginTradingOperationsLogRepository(new AzureTableStorage<OperationLogEntity>(_settings.Db.LogsConnString, "MarginTradingBackendOperationsLog", _log))
			).SingleInstance();

			builder.Register<IClientSettingsRepository>(ctx =>
				AzureRepoFactories.Clients.CreateTraderSettingsRepository(_settings.Db.ClientPersonalInfoConnString, _log)
			).SingleInstance();

			builder.Register<IClientAccountsRepository>(ctx =>
				AzureRepoFactories.Clients.CreateClientsRepository(_settings.Db.ClientPersonalInfoConnString, _log)
			).SingleInstance();

			builder.Register<IMarginTradingAccountsRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateAccountsRepository(_settings.Db.MarginTradingConnString, _log)
			).SingleInstance();

			builder.Register<IMarginTradingOrdersHistoryRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateOrdersHistoryRepository(_settings.Db.HistoryConnString, _log)
			).SingleInstance();

			builder.Register<IMarginTradingAccountHistoryRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateAccountHistoryRepository(_settings.Db.HistoryConnString, _log)
			).SingleInstance();

			builder.Register<IMatchingEngineRoutesRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateMatchingEngineRoutesRepository(_settings.Db.MarginTradingConnString, _log)
			).SingleInstance();

			builder.Register<IMarginTradingConditionRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateTradingConditionsRepository(_settings.Db.MarginTradingConnString, _log)
			).SingleInstance();

			builder.Register<IMarginTradingAccountGroupRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateAccountGroupRepository(_settings.Db.MarginTradingConnString, _log)
			).SingleInstance();

			builder.Register<IMarginTradingAccountAssetRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateAccountAssetsRepository(_settings.Db.MarginTradingConnString, _log)
			).SingleInstance();

			builder.Register<IMarginTradingAssetsRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateAssetsRepository(_settings.Db.DictsConnString, _log)
			).SingleInstance();

			builder.Register<IMarginTradingBlobRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateBlobRepository(_settings.Db.StateConnString)
			).SingleInstance();

			builder.Register<IServiceMonitoringRepository>(ctx =>
				AzureRepoFactories.Monitoring.CreateServiceMonitoringRepository(_settings.Db.SharedStorageConnString, _log)
			).SingleInstance();

			builder.Register<IAppGlobalSettingsRepositry>(ctx =>
				AzureRepoFactories.Settings.CreateAppGlobalSettingsRepository(_settings.Db.ClientPersonalInfoConnString, _log)
			).SingleInstance();

			builder.RegisterType<MatchingEngineInMemoryRepository>()
				.As<IMatchingEngineRepository>()
				.SingleInstance();
		}
	}
}
