using Autofac;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Entities;
using MarginTrading.AzureRepositories.Logs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Modules
{
    public class BackendRepositoriesModule : Module
    {
        private readonly IReloadingManager<MarginTradingSettings> _settings;
        private readonly ILog _log;

        public BackendRepositoriesModule(IReloadingManager<MarginTradingSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log).As<ILog>().SingleInstance();

            builder.Register<IMarginTradingOperationsLogRepository>(ctx =>
                    new MarginTradingOperationsLogRepository(AzureTableStorage<OperationLogEntity>.Create(
                        _settings.Nested(s => s.Db.LogsConnString), "MarginTradingBackendOperationsLog", _log)))
                .SingleInstance();

            builder.Register<IOrdersHistoryRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateOrdersHistoryRepository(
                    _settings.Nested(s => s.Db.HistoryConnString), _log)).SingleInstance();

            builder.Register<IMarginTradingAccountHistoryRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountHistoryRepository(
                    _settings.Nested(s => s.Db.HistoryConnString), _log)).SingleInstance();

            builder.Register<IMarginTradingBlobRepository>(ctx =>
                    AzureRepoFactories.MarginTrading.CreateBlobRepository(_settings.Nested(s => s.Db.StateConnString)))
                .SingleInstance();

            builder.Register<IRiskSystemCommandsLogRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateRiskSystemCommandsLogRepository(
                    _settings.Nested(s => s.Db.LogsConnString), _log)).SingleInstance();

            builder.Register(ctx =>
                AzureRepoFactories.MarginTrading.CreateDayOffSettingsRepository(
                    _settings.Nested(s => s.Db.MarginTradingConnString))).SingleInstance();

            builder.RegisterType<MatchingEngineInMemoryRepository>().As<IMatchingEngineRepository>().SingleInstance();

            builder.Register(c =>
            {
                var settings = c.Resolve<IReloadingManager<MarginTradingSettings>>();

                return settings.CurrentValue.UseAzureIdentityGenerator
                    ? (IIdentityGenerator) new AzureIdentityGenerator(
                        AzureTableStorage<IdentityEntity>.Create(settings.Nested(s => s.Db.MarginTradingConnString),
                            "Identity", _log))
                    : (IIdentityGenerator) new FakeIdentityGenerator();
            }).As<IIdentityGenerator>().SingleInstance();

            builder.Register(ctx =>
                    AzureRepoFactories.MarginTrading.CreateOrdersByIdRepository(
                        _settings.Nested(s => s.Db.MarginTradingConnString), _log, ctx.Resolve<IConvertService>()))
                .SingleInstance();

            builder.Register<IMarginTradingAccountStatsRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountStatsRepository(
                    _settings.Nested(s => s.Db.HistoryConnString), _log)).SingleInstance();
        }
    }
}