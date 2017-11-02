using Autofac;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common.Log;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.AzureRepositories.Logs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Common.Services;
using MarginTrading.Common.Settings.Repositories;
using MarginTrading.Common.Settings.Repositories.Azure;
using MarginTrading.Common.Settings.Repositories.Azure.Entities;

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
		        new MarginTradingOperationsLogRepository(
		            AzureTableStorage<OperationLogEntity>.Create(() => _settings.Db.LogsConnString,
		                "MarginTradingBackendOperationsLog", _log))
		    ).SingleInstance();

			builder.Register<IClientSettingsRepository>(ctx =>
				new ClientSettingsRepository(
					AzureTableStorage<ClientSettingsEntity>.Create(
						() => _settings.Db.ClientPersonalInfoConnString, "TraderSettings", _log)));

			builder.Register<IClientAccountsRepository>(ctx =>
				new ClientsRepository(
					AzureTableStorage<ClientAccountEntity>.Create(
						() => _settings.Db.ClientPersonalInfoConnString, "Traders", _log),
					AzureTableStorage<AzureIndex>.Create(
						() => _settings.Db.ClientPersonalInfoConnString, "Traders", _log)));

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

			builder.Register<ITradingConditionRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateTradingConditionsRepository(_settings.Db.MarginTradingConnString, _log)
			).SingleInstance();

			builder.Register<IAccountGroupRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateAccountGroupRepository(_settings.Db.MarginTradingConnString, _log)
			).SingleInstance();

			builder.Register<IAccountAssetPairsRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateAccountAssetsRepository(_settings.Db.MarginTradingConnString, _log)
			).SingleInstance();

			builder.Register<IAssetPairsRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateAssetsRepository(_settings.Db.DictsConnString, _log)
			).SingleInstance();

			builder.Register<IMarginTradingBlobRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateBlobRepository(_settings.Db.StateConnString)
			).SingleInstance();

			builder.Register<IAppGlobalSettingsRepositry>(ctx =>
				new AppGlobalSettingsRepository(AzureTableStorage<AppGlobalSettingsEntity>.Create(
					() => _settings.Db.ClientPersonalInfoConnString, "Setup", _log)));

			builder.Register<IRiskSystemCommandsLogRepository>(ctx =>
				AzureRepoFactories.MarginTrading.CreateRiskSystemCommandsLogRepository(_settings.Db.LogsConnString, _log)
			).SingleInstance();

			builder.RegisterType<MatchingEngineInMemoryRepository>()
				.As<IMatchingEngineRepository>()
				.SingleInstance();
		}
	}
}
