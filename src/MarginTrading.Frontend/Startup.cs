using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Flurl.Http;
using Lykke.Common;
using Lykke.Logs;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Settings;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.ClientContracts;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Wamp;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Settings;
using MarginTrading.Frontend.Services;
using MarginTrading.Frontend.Settings;
using MarginTrading.Services;
using MarginTrading.Services.Generated.ClientAccountServiceApi;
using MarginTrading.Services.Generated.SessionServiceApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.Swagger.Model;
using WampSharp.AspNetCore.WebSockets.Server;
using WampSharp.Binding;
using WampSharp.V2;
using WampSharp.V2.MetaApi;
using WampSharp.V2.Realm;
#pragma warning disable 1591

namespace MarginTrading.Frontend
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; set; }

        public Startup(IHostingEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.dev.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            ILoggerFactory loggerFactory = new LoggerFactory()
                .AddConsole()
                .AddDebug();

            services.AddSingleton(loggerFactory);
            services.AddLogging();
            services.AddSingleton(Configuration);
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
                });

            services.AddSwaggerGen(options =>
            {
                options.SingleApiVersion(new Info
                {
                    Version = "v1",
                    Title = "Api"
                });
                options.DescribeAllEnumsAsStrings();
            });

            var builder = new ContainerBuilder();

            ApplicationSettings appSettings = Environment.IsDevelopment()
                ? Configuration.Get<ApplicationSettings>()
                : Configuration["SettingsUrl"].GetJsonAsync<ApplicationSettings>().Result;

            MtFrontendSettings settings = appSettings.MtFrontend;

            if (!string.IsNullOrEmpty(Configuration["Env"]))
            {
                settings.MarginTradingFront.Env = Configuration["Env"];
            }

            Console.WriteLine($"Env: {settings.MarginTradingFront.Env}");

            RegisterDependencies(builder, settings, appSettings.MtFrontend.MarginTradingFront);

            builder.Populate(services);

            ApplicationContainer = builder.Build();

            SetSubscribers(settings);

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            IWampHost host = ApplicationContainer.Resolve<IWampHost>();
            IWampHostedRealm realm = ApplicationContainer.Resolve<IWampHostedRealm>();
            IDisposable realmMetaService = realm.HostMetaApiService();

            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "Default", template: "{controller}/{action}/{id?}");
            });

            app.UseSwagger();
            app.UseSwaggerUi();

            app.Map("/ws", builder =>
            {
                builder.UseWebSockets(new WebSocketOptions {KeepAliveInterval = TimeSpan.FromMinutes(1)});

                host.RegisterTransport(new AspNetCoreWebSocketTransport(builder),
                                       new JTokenJsonBinding(),
                                       new JTokenMsgpackBinding());
            });

            appLifetime.ApplicationStopped.Register(() => ApplicationContainer.Dispose());

            Application application = app.ApplicationServices.GetService<Application>();

            appLifetime.ApplicationStarted.Register(() =>
                application.StartAsync().Wait()
            );

            appLifetime.ApplicationStopping.Register(() =>
                {
                    realmMetaService.Dispose();
                    application.Stop();
                }
            );

            host.Open();
        }

        private void RegisterDependencies(ContainerBuilder builder, MtFrontendSettings settings, MtFrontSettings frontSettings)
        {
            var host = new WampHost();
            var realm = host.RealmContainer.GetRealmByName(RealmNames.FrontEnd);

            builder.RegisterInstance(host)
                .As<IWampHost>()
                .SingleInstance();

            builder.RegisterInstance(realm)
                .As<IWampHostedRealm>()
                .SingleInstance();

            LykkeLogToAzureStorage log = new LykkeLogToAzureStorage(PlatformServices.Default.Application.ApplicationName,
                new AzureTableStorage<LogEntity>(settings.MarginTradingFront.Db.LogsConnString, "MarginTradingFrontendLog", null));

            builder.RegisterInstance((ILog)log)
                .As<ILog>()
                .SingleInstance();

            builder.Register<IMarginTradingOperationsLogRepository>(ctx =>
                new MarginTradingOperationsLogRepository(new AzureTableStorage<OperationLogEntity>(settings.MarginTradingFront.Db.MarginTradingConnString, "MarginTradingFrontendOperationsLog", log))
            )
            .SingleInstance();

            builder.Register<IClientSettingsRepository>(ctx =>
                AzureRepoFactories.Clients.CreateTraderSettingsRepository(settings.MarginTradingFront.Db.ClientPersonalInfoConnString, log)
            ).SingleInstance();

            builder.Register<IAppGlobalSettingsRepositry>(ctx =>
                new AppGlobalSettingsRepository(new AzureTableStorage<AppGlobalSettingsEntity>(settings.MarginTradingFront.Db.ClientPersonalInfoConnString, "Setup", log))
            ).SingleInstance();

            builder.Register<IMarginTradingAccountsRepository>(ctx =>
               AzureRepoFactories.MarginTrading.CreateAccountsRepository(settings.MarginTradingFront.Db.MarginTradingConnString, log)
           ).SingleInstance();

            builder.Register<IMarginTradingConditionRepository>(ctx =>
               AzureRepoFactories.MarginTrading.CreateTradingConditionsRepository(settings.MarginTradingFront.Db.MarginTradingConnString, log)
           ).SingleInstance();

            builder.Register<IMarginTradingAccountAssetRepository>(ctx =>
                AzureRepoFactories.MarginTrading.CreateAccountAssetsRepository(settings.MarginTradingFront.Db.DictsConnString, log)
            ).SingleInstance();

            builder.Register<IMarginTradingWatchListRepository>(ctx =>
               AzureRepoFactories.MarginTrading.CreateWatchListsRepository(settings.MarginTradingFront.Db.MarginTradingConnString, log)
           ).SingleInstance();

            builder.RegisterType<WatchListService>()
                .As<IWatchListService>()
                .SingleInstance();

            builder.RegisterType<ClientTokenService>()
                .As<IClientTokenService>()
                .SingleInstance();

            builder.RegisterType<ClientNotificationService>()
                .As<IClientNotificationService>()
                .SingleInstance();

            builder.RegisterType<MarginTradingOperationsLogService>()
                .As<IMarginTradingOperationsLogService>()
                .SingleInstance();

            var consoleWriter = new ConsoleLWriter(line =>
                {
                    try
                    {
                        if (settings.MarginTradingFront.RemoteConsoleEnabled && !string.IsNullOrEmpty(settings.MarginTradingFront.MetricLoggerLine))
                        {
                            settings.MarginTradingFront.MetricLoggerLine.PostJsonAsync(
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

            builder.RegisterInstance(settings)
                .SingleInstance();

            builder.RegisterInstance(frontSettings)
                .SingleInstance();

            builder.RegisterType<RpcMtFrontend>()
                .As<IRpcMtFrontend>()
                .SingleInstance();

            builder.RegisterType<HttpRequestService>()
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

            builder.Register<ISessionService>(ctx =>
                new SessionService(new Uri(settings.MarginTradingFront.SessionServiceApiUrl))
            ).SingleInstance();

            builder.Register<IClientAccountService>(ctx =>
                new ClientAccountService(new Uri(settings.MarginTradingFront.ClientAccountApiUrl))
            ).SingleInstance();
        }

        private void SetSubscribers(MtFrontendSettings settings)
        {
            MarginTradingBackendServiceLocator.RabbitMqHandler = ApplicationContainer.Resolve<RabbitMqHandler>();
            var log = ApplicationContainer.Resolve<ILog>();
            var consoleWriter = ApplicationContainer.Resolve<IConsole>();

            MarginTradingBackendServiceLocator.SubscriberPrices = new RabbitMqSubscriber<InstrumentBidAskPair>(new RabbitMqSubscriberSettings
            {
                ConnectionString = settings.MarginTradingLive.MarginTradingRabbitMqSettings.ConnectionString,
                ExchangeName = settings.MarginTradingLive.MarginTradingRabbitMqSettings.ExchangeName,
                QueueName = settings.MarginTradingFront.RabbitMqQueues.OrderbookPrices.QueueName + ".frontend",
                IsDurable = false
            })
                .SetMessageDeserializer(new FrontEndDeserializer<InstrumentBidAskPair>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(settings.MarginTradingFront.RabbitMqQueues.OrderbookPrices.RoutingKeyName))
                .SetLogger(log)
                .SetConsole(consoleWriter)
                .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessPrices)
                .Start();

            MarginTradingBackendServiceLocator.SubscriberAccountChangedDemo = new RabbitMqSubscriber<MarginTradingAccountBackendContract>(new RabbitMqSubscriberSettings
            {
                ConnectionString = settings.MarginTradingDemo.MarginTradingRabbitMqSettings.ConnectionString,
                ExchangeName = settings.MarginTradingDemo.MarginTradingRabbitMqSettings.ExchangeName,
                QueueName = settings.MarginTradingFront.RabbitMqQueues.AccountChanged.QueueName + ".frontend",
                IsDurable = false
            })
                .SetMessageDeserializer(new FrontEndDeserializer<MarginTradingAccountBackendContract>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(settings.MarginTradingFront.RabbitMqQueues.AccountChanged.RoutingKeyName))
                .SetLogger(log)
                .SetConsole(consoleWriter)
                .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessAccountChanged)
                .Start();

            MarginTradingBackendServiceLocator.SubscriberAccountChangedLive = new RabbitMqSubscriber<MarginTradingAccountBackendContract>(new RabbitMqSubscriberSettings
            {
                ConnectionString = settings.MarginTradingLive.MarginTradingRabbitMqSettings.ConnectionString,
                ExchangeName = settings.MarginTradingLive.MarginTradingRabbitMqSettings.ExchangeName,
                QueueName = settings.MarginTradingFront.RabbitMqQueues.AccountChanged.QueueName + ".frontend",
                IsDurable = false
            })
                .SetMessageDeserializer(new FrontEndDeserializer<MarginTradingAccountBackendContract>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(settings.MarginTradingFront.RabbitMqQueues.AccountChanged.RoutingKeyName))
                .SetLogger(log)
                .SetConsole(consoleWriter)
                .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessAccountChanged)
                .Start();

            MarginTradingBackendServiceLocator.SubscriberOrderChangedDemo = new RabbitMqSubscriber<OrderContract>(new RabbitMqSubscriberSettings
            {
                ConnectionString = settings.MarginTradingDemo.MarginTradingRabbitMqSettings.ConnectionString,
                ExchangeName = settings.MarginTradingDemo.MarginTradingRabbitMqSettings.ExchangeName,
                QueueName = settings.MarginTradingFront.RabbitMqQueues.OrderChanged.QueueName + ".frontend",
                IsDurable = false
            })
                .SetMessageDeserializer(new FrontEndDeserializer<OrderContract>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(settings.MarginTradingFront.RabbitMqQueues.OrderChanged.RoutingKeyName))
                .SetLogger(log)
                .SetConsole(consoleWriter)
                .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessOrderChanged)
                .Start();

            MarginTradingBackendServiceLocator.SubscriberOrderChangedLive = new RabbitMqSubscriber<OrderContract>(new RabbitMqSubscriberSettings
            {
                ConnectionString = settings.MarginTradingLive.MarginTradingRabbitMqSettings.ConnectionString,
                ExchangeName = settings.MarginTradingLive.MarginTradingRabbitMqSettings.ExchangeName,
                QueueName = settings.MarginTradingFront.RabbitMqQueues.OrderChanged.QueueName + ".frontend",
                IsDurable = false
            })
                .SetMessageDeserializer(new FrontEndDeserializer<OrderContract>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(settings.MarginTradingFront.RabbitMqQueues.OrderChanged.RoutingKeyName))
                .SetLogger(log)
                .SetConsole(consoleWriter)
                .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessOrderChanged)
                .Start();

            MarginTradingBackendServiceLocator.SubscriberAccountStopoutDemo = new RabbitMqSubscriber<AccountStopoutBackendContract>(new RabbitMqSubscriberSettings
            {
                ConnectionString = settings.MarginTradingDemo.MarginTradingRabbitMqSettings.ConnectionString,
                ExchangeName = settings.MarginTradingDemo.MarginTradingRabbitMqSettings.ExchangeName,
                QueueName = settings.MarginTradingFront.RabbitMqQueues.AccountStopout.QueueName + ".frontend",
                IsDurable = false
            })
                .SetMessageDeserializer(new FrontEndDeserializer<AccountStopoutBackendContract>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(settings.MarginTradingFront.RabbitMqQueues.AccountStopout.RoutingKeyName))
                .SetLogger(log)
                .SetConsole(consoleWriter)
                .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessAccountStopout)
                .Start();

            MarginTradingBackendServiceLocator.SubscriberAccountStopoutLive = new RabbitMqSubscriber<AccountStopoutBackendContract>(new RabbitMqSubscriberSettings
            {
                ConnectionString = settings.MarginTradingLive.MarginTradingRabbitMqSettings.ConnectionString,
                ExchangeName = settings.MarginTradingLive.MarginTradingRabbitMqSettings.ExchangeName,
                QueueName = settings.MarginTradingFront.RabbitMqQueues.AccountStopout.QueueName + ".frontend",
                IsDurable = false
            })
                .SetMessageDeserializer(new FrontEndDeserializer<AccountStopoutBackendContract>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(settings.MarginTradingFront.RabbitMqQueues.AccountStopout.RoutingKeyName))
                .SetLogger(log)
                .SetConsole(consoleWriter)
                .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessAccountStopout)
                .Start();

            MarginTradingBackendServiceLocator.SubscribeUserUpdatesDemo = new RabbitMqSubscriber<UserUpdateEntityBackendContract>(new RabbitMqSubscriberSettings
            {
                ConnectionString = settings.MarginTradingDemo.MarginTradingRabbitMqSettings.ConnectionString,
                ExchangeName = settings.MarginTradingDemo.MarginTradingRabbitMqSettings.ExchangeName,
                QueueName = settings.MarginTradingFront.RabbitMqQueues.UserUpdates.QueueName + ".frontend",
                IsDurable = false
            })
                .SetMessageDeserializer(new FrontEndDeserializer<UserUpdateEntityBackendContract>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(settings.MarginTradingFront.RabbitMqQueues.UserUpdates.RoutingKeyName))
                .SetLogger(log)
                .SetConsole(consoleWriter)
                .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessUserUpdates)
                .Start();

            MarginTradingBackendServiceLocator.SubscribeUserUpdatesLive = new RabbitMqSubscriber<UserUpdateEntityBackendContract>(new RabbitMqSubscriberSettings
            {
                ConnectionString = settings.MarginTradingLive.MarginTradingRabbitMqSettings.ConnectionString,
                ExchangeName = settings.MarginTradingLive.MarginTradingRabbitMqSettings.ExchangeName,
                QueueName = settings.MarginTradingFront.RabbitMqQueues.UserUpdates.QueueName + ".frontend",
                IsDurable = false
            })
                .SetMessageDeserializer(new FrontEndDeserializer<UserUpdateEntityBackendContract>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(settings.MarginTradingFront.RabbitMqQueues.UserUpdates.RoutingKeyName))
                .SetLogger(log)
                .SetConsole(consoleWriter)
                .Subscribe(MarginTradingBackendServiceLocator.RabbitMqHandler.ProcessUserUpdates)
                .Start();
        }
    }

    public static class MarginTradingBackendServiceLocator
    {
        public static RabbitMqHandler RabbitMqHandler;
        public static RabbitMqSubscriber<InstrumentBidAskPair> SubscriberPrices;
        public static RabbitMqSubscriber<MarginTradingAccountBackendContract> SubscriberAccountChangedDemo;
        public static RabbitMqSubscriber<MarginTradingAccountBackendContract> SubscriberAccountChangedLive;
        public static RabbitMqSubscriber<OrderContract> SubscriberOrderChangedDemo;
        public static RabbitMqSubscriber<OrderContract> SubscriberOrderChangedLive;
        public static RabbitMqSubscriber<AccountStopoutBackendContract> SubscriberAccountStopoutDemo;
        public static RabbitMqSubscriber<AccountStopoutBackendContract> SubscriberAccountStopoutLive;
        public static RabbitMqSubscriber<UserUpdateEntityBackendContract> SubscribeUserUpdatesDemo;
        public static RabbitMqSubscriber<UserUpdateEntityBackendContract> SubscribeUserUpdatesLive;
    }
}
