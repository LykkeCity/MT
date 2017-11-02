using Autofac;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Common.Settings.Repositories;
using MarginTrading.Common.Settings.Repositories.Azure;
using MarginTrading.Common.Settings.Repositories.Azure.Entities;
using MarginTrading.DataReader.Settings;

namespace MarginTrading.DataReader.Modules
{
    public class DataReaderRepositoriesModule : Module
    {
        private readonly DataReaderSettings _settings;
        private readonly ILog _log;

        public DataReaderRepositoriesModule(DataReaderSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.Register<IClientSettingsRepository>(ctx =>
                new ClientSettingsRepository(
                    AzureTableStorage<ClientSettingsEntity>.Create(
                        () => _settings.Db.ClientPersonalInfoConnString, "TraderSettings", _log)));

            builder.Register<IMarginTradingAccountsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountsRepository(_settings.Db.MarginTradingConnString, _log)
            ).SingleInstance();

            builder.Register<IMarginTradingAccountStatsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountStatsRepository(_settings.Db.HistoryConnString, _log)
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
        }
    }
}
