using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using AzureStorage.Tables;
using Common.Log;
using Flurl.Http;
using Lykke.Common;
using Lykke.Service.Session;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Settings;
using MarginTrading.Common.Wamp;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Settings;
using MarginTrading.DataReaderClient;
using MarginTrading.Frontend.Services;
using MarginTrading.Frontend.Settings;
using MarginTrading.Services;
using MarginTrading.Services.Infrastructure;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Rest;
using Rocks.Caching;
using WampSharp.V2;
using WampSharp.V2.Realm;

namespace MarginTrading.Frontend.Modules
{
    public class FrontendModule: Module
    {
        private readonly MtFrontendSettings _settings;

        public FrontendModule(MtFrontendSettings settings)
        {
            this._settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var host = new WampHost();
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
                        () => _settings.MarginTradingFront.Db.LogsConnString, "MarginTradingFrontendOperationsLog",
                        LogLocator.CommonLog))
                )
                .SingleInstance();

            builder.Register<IClientSettingsRepository>(ctx =>
                AzureRepoFactories.Clients.CreateTraderSettingsRepository(_settings.MarginTradingFront.Db.ClientPersonalInfoConnString, LogLocator.CommonLog)
            ).SingleInstance();

            builder.Register<IClientAccountsRepository>(ctx =>
                AzureRepoFactories.Clients.CreateClientsRepository(_settings.MarginTradingFront.Db.ClientPersonalInfoConnString, LogLocator.CommonLog)
            ).SingleInstance();

            builder.Register<IAppGlobalSettingsRepositry>(ctx =>
                new AppGlobalSettingsRepository(AzureTableStorage<AppGlobalSettingsEntity>.Create(
                    () => _settings.MarginTradingFront.Db.ClientPersonalInfoConnString, "Setup", LogLocator.CommonLog))
            ).SingleInstance();

            builder.Register<IMarginTradingWatchListRepository>(ctx =>
               AzureRepoFactories.MarginTrading.CreateWatchListsRepository(_settings.MarginTradingFront.Db.MarginTradingConnString, LogLocator.CommonLog)
           ).SingleInstance();

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

            var consoleWriter = new ConsoleLWriter(line =>
            {
                try
                {
                    if (_settings.MarginTradingFront.RemoteConsoleEnabled && !string.IsNullOrEmpty(_settings.MarginTradingFront.MetricLoggerLine))
                    {
                        _settings.MarginTradingFront.MetricLoggerLine.PostJsonAsync(
                            new
                            {
                                Id = "Mt-frontend",
                                Data =
                                new[]
                                {
                                        new { Key = "Version", Value = PlatformServices.Default.Application.ApplicationVersion },
                                        new { Key = "Data", Value = line }
                                }
                            });
                    }
                }
                catch { }
            });

            builder.RegisterInstance(consoleWriter)
                .As<IConsole>()
                .SingleInstance();

            builder.RegisterType<RabbitMqHandler>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterInstance(_settings)
                .SingleInstance();

            builder.RegisterInstance(_settings.MarginTradingFront)
                .SingleInstance();

            builder.RegisterInstance(_settings.MarginTradingFront.RequestLoggerSettings)
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
                new ClientSessionsClient(_settings.MarginTradingFront.SessionServiceApiUrl, LogLocator.CommonLog)
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
                        _settings.MarginTradingFront.DataReaderApiSettings.DemoApiUrl,
                        _settings.MarginTradingFront.DataReaderApiSettings.LiveApiUrl,
                        _settings.MarginTradingFront.DataReaderApiSettings.DemoApiKey,
                        _settings.MarginTradingFront.DataReaderApiSettings.LiveApiKey,
                        "MarginTradingFrontend"))
                .SingleInstance();
        }
    }
}
