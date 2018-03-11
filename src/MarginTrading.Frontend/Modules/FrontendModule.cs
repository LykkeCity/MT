using System;
using Autofac;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Common;
using Lykke.Service.Session;
using Lykke.SettingsReader;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services;
using MarginTrading.Common.Services.Client;
using MarginTrading.Common.Services.Settings;
using MarginTrading.Frontend.Repositories;
using MarginTrading.Frontend.Repositories.Contract;
using MarginTrading.Frontend.Repositories.Entities;
using MarginTrading.Frontend.Services;
using MarginTrading.Frontend.Settings;
using MarginTrading.Frontend.Wamp;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Rocks.Caching;
using WampSharp.V2;
using WampSharp.V2.Realm;
using MarginTradingOperationsLogRepository = MarginTrading.Frontend.Repositories.MarginTradingOperationsLogRepository;
using OperationLogEntity = MarginTrading.Frontend.Repositories.Entities.OperationLogEntity;

namespace MarginTrading.Frontend.Modules
{
    public class FrontendModule: Module
    {
        private readonly IReloadingManager<MtFrontendSettings> _settings;

        public FrontendModule(IReloadingManager<MtFrontendSettings> settings)
        {
            this._settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var host = new WampAuthenticationHost(new WampSessionAuthenticatorFactory());
            var realm = host.RealmContainer.GetRealmByName(WampConstants.FrontEndRealmName);

            builder.RegisterInstance(host)
                .As<IWampHost>()
                .SingleInstance();

            builder.RegisterInstance(realm)
                .As<IWampHostedRealm>()
                .SingleInstance();

            builder.RegisterInstance(LogLocator.CommonLog)
                .As<ILog>()
                .SingleInstance();

            builder.Register<IMarginTradingOperationsLogRepository>(ctx =>
                    new MarginTradingOperationsLogRepository(AzureTableStorage<OperationLogEntity>.Create(
                        _settings.Nested(s => s.MarginTradingFront.Db.LogsConnString), "MarginTradingFrontendOperationsLog",
                        LogLocator.CommonLog))
                )
                .SingleInstance();

            builder.Register<IMarginTradingWatchListRepository>(ctx =>
                new MarginTradingWatchListsRepository(AzureTableStorage<MarginTradingWatchListEntity>.Create(
                    _settings.Nested(s => s.MarginTradingFront.Db.MarginTradingConnString),
                    "MarginTradingWatchLists", LogLocator.CommonLog)));

            builder.Register<IMaintenanceInfoRepository>(ctx =>
                new MaintenanceInfoRepository(_settings.Nested(s => s.MarginTradingFront.Db.MarginTradingConnString)));

            builder.RegisterType<WatchListService>()
                .As<IWatchListService>()
                .SingleInstance();

            builder.RegisterType<ClientTokenService>()
                .As<IClientTokenService>()
                .SingleInstance();

            builder.RegisterType<ClientAccountService>()
                .As<IClientAccountService>()
                .SingleInstance();

            builder.RegisterType<MarginTradingOperationsLogService>()
                .As<IMarginTradingOperationsLogService>()
                .SingleInstance();

            var consoleWriter = new ConsoleLWriter(Console.WriteLine);

            builder.RegisterInstance(consoleWriter)
                .As<IConsole>()
                .SingleInstance();

            builder.RegisterType<RabbitMqHandler>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterInstance(_settings.CurrentValue)
                .SingleInstance();

            builder.RegisterInstance(_settings.CurrentValue.MarginTradingFront)
                .SingleInstance();

            builder.RegisterInstance(_settings.CurrentValue.MarginTradingFront.RequestLoggerSettings)
                .SingleInstance();
            
            builder.RegisterInstance(_settings.CurrentValue.MarginTradingFront.CorsSettings)
                .SingleInstance();
            
            builder.RegisterInstance(_settings.CurrentValue.MarginTradingFront.TerminalsSettings)
                .SingleInstance();

            builder.RegisterType<RpcMtFrontend>()
                .As<IRpcMtFrontend>()
                .SingleInstance();

            builder.RegisterType<HttpContextAccessor>()
                .As<IHttpContextAccessor>()
                .SingleInstance();
            
            builder.RegisterType<TerminalInfoService>()
                .As<ITerminalInfoService>()
                .SingleInstance();
            
            builder.RegisterType<HttpRequestService>()
                .As<IHttpRequestService>()
                .SingleInstance();

            builder.RegisterType<MarginTradingEnabledCacheService>()
               .As<IMarginTradingSettingsCacheService>()
               .SingleInstance();

            builder.RegisterType<ThreadSwitcherToNewTask>()
                .As<IThreadSwitcher>()
                .SingleInstance();

            builder.RegisterType<Application>()
                .SingleInstance();

            builder.Register<IClientsSessionsRepository>(ctx =>
                new ClientSessionsClient(_settings.CurrentValue.MarginTradingFront.SessionServiceApiUrl, LogLocator.CommonLog)
            ).SingleInstance();

            builder.RegisterType<ClientTokenValidator>()
                .As<ISecurityTokenValidator>()
                .SingleInstance();

            builder.RegisterType<WampSessionsService>()
                .AsSelf()
                .SingleInstance();


            builder.RegisterType<RpcFacade>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<MemoryCacheProvider>()
                   .As<ICacheProvider>()
                   .AsSelf()
                   .SingleInstance();

            builder.Register(context =>
                    MarginTradingDataReaderApiClientFactory.CreateDefaultClientsPair(
                        _settings.CurrentValue.MarginTradingFront.DataReaderApiSettings.DemoApiUrl,
                        _settings.CurrentValue.MarginTradingFront.DataReaderApiSettings.LiveApiUrl,
                        _settings.CurrentValue.MarginTradingFront.DataReaderApiSettings.DemoApiKey,
                        _settings.CurrentValue.MarginTradingFront.DataReaderApiSettings.LiveApiKey,
                        "MarginTradingFrontend"))
                .SingleInstance();
            
            builder.RegisterType<DateService>()
                .As<IDateService>()
                .SingleInstance();
            
            builder.Register(c => new RabbitMqService(c.Resolve<ILog>(), c.Resolve<IConsole>(),
                    null, _settings.CurrentValue.MarginTradingFront.Env))
                .As<IRabbitMqService>()
                .SingleInstance();
        }
    }
}
