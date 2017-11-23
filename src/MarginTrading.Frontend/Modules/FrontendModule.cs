using System;
using Autofac;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common.Log;
using Lykke.Common;
using Lykke.Service.Session;
using Lykke.SettingsReader;
using MarginTrading.Common.Services;
using MarginTrading.Common.Settings;
using MarginTrading.Common.Settings.Repositories;
using MarginTrading.Common.Settings.Repositories.Azure;
using MarginTrading.Common.Settings.Repositories.Azure.Entities;
using MarginTrading.DataReaderClient;
using MarginTrading.Frontend.Repositories;
using MarginTrading.Frontend.Services;
using MarginTrading.Frontend.Settings;
using MarginTrading.Frontend.Wamp;
using Microsoft.IdentityModel.Tokens;
using Rocks.Caching;
using WampSharp.V2;
using WampSharp.V2.Realm;
using MarginTradingOperationsLogRepository = MarginTrading.Frontend.Repositories.MarginTradingOperationsLogRepository;
using OperationLogEntity = MarginTrading.Frontend.Repositories.OperationLogEntity;

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
            var realm = host.RealmContainer.GetRealmByName(RealmNames.FrontEnd);

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

            builder.Register<IClientSettingsRepository>(ctx =>
                new ClientSettingsRepository(
                    AzureTableStorage<ClientSettingsEntity>.Create(
                        _settings.Nested(s => s.MarginTradingFront.Db.ClientPersonalInfoConnString), "TraderSettings",
                        LogLocator.CommonLog)));



            builder.Register<IClientAccountsRepository>(ctx =>
                new ClientsRepository(
                    AzureTableStorage<ClientAccountEntity>.Create(
                        _settings.Nested(s => s.MarginTradingFront.Db.ClientPersonalInfoConnString), "Traders",
                        LogLocator.CommonLog),
                    AzureTableStorage<AzureIndex>.Create(
                        _settings.Nested(s => s.MarginTradingFront.Db.ClientPersonalInfoConnString), "Traders",
                        LogLocator.CommonLog)));

            builder.Register<IAppGlobalSettingsRepositry>(ctx =>
                new AppGlobalSettingsRepository(AzureTableStorage<AppGlobalSettingsEntity>.Create(
                    _settings.Nested(s => s.MarginTradingFront.Db.ClientPersonalInfoConnString), "Setup", LogLocator.CommonLog))
            ).SingleInstance();

            builder.Register<IMarginTradingWatchListRepository>(ctx =>
                new MarginTradingWatchListsRepository(AzureTableStorage<MarginTradingWatchListEntity>.Create(
                    _settings.Nested(s => s.MarginTradingFront.Db.MarginTradingConnString),
                    "MarginTradingWatchLists", LogLocator.CommonLog)));

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

            builder.RegisterType<RpcMtFrontend>()
                .As<IRpcMtFrontend>()
                .SingleInstance();

            builder.RegisterType<HttpRequestService>()
                .As<IHttpRequestService>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<MarginTradingSettingsService>()
               .As<IMarginTradingSettingsService>()
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
        }
    }
}
