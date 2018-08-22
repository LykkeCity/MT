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
using MarginTrading.SqlRepositories.Repositories;
using MarginTrading.SqlRepositories;
using Microsoft.Extensions.Internal;
using OperationLogEntity = MarginTrading.AzureRepositories.OperationLogEntity;

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

            if (_settings.CurrentValue.Db.StorageMode == StorageMode.Azure)
            {
                builder.Register<IOperationsLogRepository>(ctx =>
                        new OperationsLogRepository(AzureTableStorage<OperationLogEntity>.Create(
                            _settings.Nested(s => s.Db.LogsConnString), "MarginTradingBackendOperationsLog", _log)))
                    .SingleInstance();

                builder.Register<IMarginTradingBlobRepository>(ctx =>
                        AzureRepoFactories.MarginTrading.CreateBlobRepository(
                            _settings.Nested(s => s.Db.StateConnString)))
                    .SingleInstance();

                builder.Register(c =>
                {
                    var settings = c.Resolve<IReloadingManager<MarginTradingSettings>>();

                    return settings.CurrentValue.UseDbIdentityGenerator
                        ? (IIdentityGenerator) new AzureIdentityGenerator(
                            AzureTableStorage<IdentityEntity>.Create(settings.Nested(s => s.Db.MarginTradingConnString),
                                "Identity", _log))
                        : (IIdentityGenerator) new SimpleIdentityGenerator();
                }).As<IIdentityGenerator>().SingleInstance();
            }
            else if (_settings.CurrentValue.Db.StorageMode == StorageMode.SqlServer)
            {
                builder.Register<IOperationsLogRepository>(ctx =>
                        new SqlOperationsLogRepository(ctx.Resolve<IDateService>(), 
                            "MarginTradingBackendOperationsLog", _settings.CurrentValue.Db.LogsConnString))
                    .SingleInstance();

                builder.Register<IMarginTradingBlobRepository>(ctx =>
                        new SqlBlobRepository(_settings.CurrentValue.Db.StateConnString))
                    .SingleInstance();

                builder.Register(c => c.Resolve<IReloadingManager<MarginTradingSettings>>().CurrentValue
                        .UseDbIdentityGenerator
                        ? (IIdentityGenerator) new SqlIdentityGenerator()
                        : (IIdentityGenerator) new SimpleIdentityGenerator())
                    .As<IIdentityGenerator>()
                    .SingleInstance();
            }
            
            builder.RegisterType<MatchingEngineInMemoryRepository>().As<IMatchingEngineRepository>().SingleInstance();

            builder.Register(c =>
            {
                var settings = c.Resolve<IReloadingManager<MarginTradingSettings>>();

                return settings.CurrentValue.UseDbIdentityGenerator
                    ? (IIdentityGenerator) new AzureIdentityGenerator(
                        AzureTableStorage<IdentityEntity>.Create(settings.Nested(s => s.Db.MarginTradingConnString),
                            "Identity", _log))
                    : (IIdentityGenerator) new SimpleIdentityGenerator();
            }).As<IIdentityGenerator>().SingleInstance();

            builder.Register(ctx =>
                    AzureRepoFactories.MarginTrading.CreateOrdersByIdRepository(
                        _settings.Nested(s => s.Db.MarginTradingConnString), _log, ctx.Resolve<IConvertService>()))
                .SingleInstance();
            
            //SQL PLACE
            builder.RegisterType<AccountMarginFreezingRepository>()
                .As<IAccountMarginFreezingRepository>()
                .SingleInstance();
            //            builder.Register(ctx =>
//                AzureRepoFactories.MarginTrading.CreateDayOffSettingsRepository(
//                    _settings.Nested(s => s.Db.MarginTradingConnString))).SingleInstance();
            
//            builder.Register(ctx =>
//                    AzureRepoFactories.MarginTrading.CreateOrdersByIdRepository(
//                        _settings.Nested(s => s.Db.MarginTradingConnString), _log, ctx.Resolve<IConvertService>()))
//                .SingleInstance();
            
            //            builder.Register<IRiskSystemCommandsLogRepository>(ctx =>
//                AzureRepoFactories.MarginTrading.CreateRiskSystemCommandsLogRepository(
//                    _settings.Nested(s => s.Db.LogsConnString), _log)).SingleInstance();
        }
    }
}