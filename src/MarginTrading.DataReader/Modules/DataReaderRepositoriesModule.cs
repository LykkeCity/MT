using Autofac;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AzureRepositories;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Services;
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

            builder.Register<IMarginTradingAccountStatsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountStatsRepository(_settings.Nested(s => s.Db.HistoryConnString), _log)
            ).SingleInstance();

            builder.Register<IOrdersHistoryRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateOrdersHistoryRepository(_settings.Nested(s => s.Db.HistoryConnString), _log)
            ).SingleInstance();

            builder.Register<IMarginTradingAccountHistoryRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountHistoryRepository(_settings.Nested(s => s.Db.HistoryConnString), _log)
            ).SingleInstance();

            builder.Register<IMarginTradingBlobRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateBlobRepository(_settings.Nested(s => s.Db.StateConnString))
            ).SingleInstance();

            builder.Register(ctx =>
                AzureRepoFactories.MarginTrading.CreateTradesRepository(
                    _settings.Nested(s => s.Db.HistoryConnString), _log, ctx.Resolve<IConvertService>())
            ).SingleInstance();
        }
    }
}
