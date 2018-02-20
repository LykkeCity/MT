using Autofac;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.DataReader.Settings;

namespace MarginTrading.DataReader.Modules
{
    public class DataReaderRepositoriesModule : Module
    {
        private readonly IReloadingManager<DataReaderSettings> _settings;
        private readonly ILog _log;

        public DataReaderRepositoriesModule(IReloadingManager<DataReaderSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.Register<IMarginTradingAccountsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountsRepository(_settings.Nested(s => s.Db.MarginTradingConnString), _log)
            ).SingleInstance();

            builder.Register<IMarginTradingAccountStatsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountStatsRepository(_settings.Nested(s => s.Db.HistoryConnString), _log)
            ).SingleInstance();

            builder.Register<IMarginTradingOrdersHistoryRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateOrdersHistoryRepository(_settings.Nested(s => s.Db.HistoryConnString), _log)
            ).SingleInstance();

            builder.Register<IMarginTradingAccountHistoryRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountHistoryRepository(_settings.Nested(s => s.Db.HistoryConnString), _log)
            ).SingleInstance();

            builder.Register<IMatchingEngineRoutesRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateMatchingEngineRoutesRepository(_settings.Nested(s => s.Db.MarginTradingConnString), _log)
            ).SingleInstance();

            builder.Register<ITradingConditionRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateTradingConditionsRepository(_settings.Nested(s => s.Db.MarginTradingConnString), _log)
            ).SingleInstance();

            builder.Register<IAccountGroupRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountGroupRepository(_settings.Nested(s => s.Db.MarginTradingConnString), _log)
            ).SingleInstance();

            builder.Register<IAccountAssetPairsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountAssetsRepository(_settings.Nested(s => s.Db.MarginTradingConnString), _log)
            ).SingleInstance();

            builder.Register<IMarginTradingBlobRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateBlobRepository(_settings.Nested(s => s.Db.StateConnString))
            ).SingleInstance();
        }
    }
}
